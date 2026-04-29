using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection.Events;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;
using RedisGUI.Domain.Users;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents a Redis connection configuration aggregate root
/// </summary>
public abstract class RedisConnection : Entity, IAggregateRoot
{
	/// <summary>
	/// Creates a new instance of <see cref="RedisConnection"/>
	/// </summary>
	protected RedisConnection()
	{

	}

	/// <summary>
	/// Creates a new instance of <see cref="RedisConnection"/>
	/// </summary>
	protected RedisConnection(
		Guid id,
		ConnectionName connectionName,
		ConnectionHost serverHost,
		ConnectionPort serverPort,
		DatabaseNumber databaseNumber,
		ConnectionOwnerId ownerId) : base(id)
	{
		Ensure.NotNull(connectionName, "Connection name is required", nameof(connectionName));
		Ensure.NotNull(serverHost, "Server host is required", nameof(serverHost));
		Ensure.NotNull(serverPort, "Server port is required", nameof(serverPort));
		Ensure.NotNull(databaseNumber, "Database number is required", nameof(databaseNumber));
		Ensure.NotNull(ownerId, "Owner ID is required", nameof(ownerId));

		ConnectionName = connectionName;
		ServerHost = serverHost;
		ServerPort = serverPort;
		DatabaseNumber = databaseNumber;
		OwnerId = ownerId;

		RaiseDomainEvent(new ConnectionCreatedDomainEvent(id, connectionName.Value));
	}

	/// <summary>
	/// The friendly name of the connection
	/// </summary>
	public ConnectionName ConnectionName { get; private set; } = null!;

	/// <summary>
	/// The Redis server host address
	/// </summary>
	public ConnectionHost ServerHost { get; private set; } = null!;

	/// <summary>
	/// The Redis server port number
	/// </summary>
	public ConnectionPort ServerPort { get; private set; } = null!;

	/// <summary>
	/// The Redis database number to connect to
	/// </summary>
	public DatabaseNumber DatabaseNumber { get; private set; } = null!;

	/// <summary>
	/// Indicates whether connection is established.
	/// </summary>
	public bool IsAvailable { get; private set; }

	/// <summary>
	/// Gets the owner of this connection (None for legacy/public connections)
	/// </summary>
	public ConnectionOwnerId OwnerId { get; private set; } = null!;

	/// <summary>
	/// Creates a new Redis connection configuration
	/// </summary>
	/// <param name="connectionName">The friendly name for this connection</param>
	/// <param name="serverHost">The Redis server host address</param>
	/// <param name="serverPort">The Redis server port number</param>
	/// <param name="databaseNumber">The Redis database number</param>
	/// <param name="ownerId">The owner of the connection (None for public connections)</param>
	/// <param name="connectionCredentials">Optional authentication credentials</param>
	/// <returns>A new <see cref="RedisConnection"/> instance</returns>
	public static Result<RedisConnection> Create(
		ConnectionName connectionName,
		ConnectionHost serverHost,
		ConnectionPort serverPort,
		DatabaseNumber databaseNumber,
		ConnectionOwnerId ownerId,
		ConnectionCredentials? connectionCredentials = null)
	{
		try
		{
			RedisConnection? connection = null;
			Guid id = Guid.NewGuid();
			if (connectionCredentials == null)
			{
				connection = new AnonymousRedisConnection(
					Guid.NewGuid(),
					connectionName,
					serverHost,
					serverPort,
					databaseNumber,
					ownerId);
			}
			else
			{
				connection = new RedisConnectionWithCredentials(
					id,
					connectionName,
					serverHost,
					serverPort,
					databaseNumber,
					ownerId,
					connectionCredentials);
			}

			return Result.Success(connection);
		}
		catch (Exception)
		{
			return Result.Failure<RedisConnection>(DomainErrors.Connection.CreationFailed);
		}
	}


	/// <summary>
	/// Updates the connection details for the Redis server
	/// </summary>
	/// <param name="newName">The new friendly name for the connection</param>
	/// <param name="newHost">The new Redis server host address</param>
	/// <param name="newPort">The new Redis server port number</param>
	/// <param name="newDatabaseNumber">The new Redis database number</param>
	/// <returns>A result indicating the success or failure of the update operation</returns>
	public Result UpdateConnectionDetails(
		ConnectionName newName,
		ConnectionHost newHost,
		ConnectionPort newPort,
		DatabaseNumber newDatabaseNumber)
	{
		try
		{
			ConnectionName = newName;
			ServerHost = newHost;
			ServerPort = newPort;
			DatabaseNumber = newDatabaseNumber;

			return Result.Success();
		}
		catch (Exception)
		{
			return Result.Failure(DomainErrors.Connection.UpdateFailed);
		}

	}

	/// <summary>
	/// Change availability state of connection.
	/// </summary>
	/// <param name="isAvailable"></param>
	public void UpdateAvailability(bool isAvailable)
	{
		this.IsAvailable = isAvailable;
		this.RaiseDomainEvent(new ConnectionAvailabilityChanged(this.Id, this.IsAvailable));
	}

	/// <summary>
	/// Determines whether a user can access this connection based on ownership and role
	/// </summary>
	/// <param name="userId">The user's unique identifier</param>
	/// <param name="userRole">The user's role</param>
	/// <returns>True if the user can access the connection; otherwise, false</returns>
	public bool CanBeAccessedBy(Guid userId, UserRole userRole)
	{
		// Admins can access all connections
		if (userRole == UserRole.Admin)
			return true;

		// Connection without owner is public (legacy/shared)
		if (!OwnerId.HasValue)
			return true;

		// User can only access their own connections
		return OwnerId.Value == userId;
	}

	/// <summary>
	/// Sets the owner of this connection
	/// </summary>
	/// <param name="ownerId">The owner's user ID</param>
	public void SetOwner(ConnectionOwnerId ownerId)
	{
		OwnerId = ownerId;
	}

	/// <summary>
	/// Removes ownership from this connection (makes it public)
	/// </summary>
	public void RemoveOwner()
	{
		OwnerId = ConnectionOwnerId.None();
	}

	/// <summary>
	/// Builds the Redis connection string from the configuration
	/// </summary>
	/// <returns>A formatted Redis connection string</returns>
	public string BuildConnectionString()
	{
		string baseConnectionString = $"{GetBaseConnectionString()},allowAdmin=True,connectTimeout=30,abortConnect=False";

		if (!baseConnectionString.Contains("database") && DatabaseNumber.Value > 0)
		{
			baseConnectionString = $"{baseConnectionString}/database={DatabaseNumber.Value}";
		}

		return baseConnectionString;
	}

	/// <summary>
	/// Returns base connection string which contains information provided by child implementation
	/// </summary>
	/// <returns></returns>
	protected abstract string GetBaseConnectionString();
}
