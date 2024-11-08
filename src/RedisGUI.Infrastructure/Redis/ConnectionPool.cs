using Microsoft.Extensions.Logging;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Primitives;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace RedisGUI.Infrastructure.Redis;

/// <summary>
/// Implements a thread-safe connection pool for managing Redis connections
/// </summary>
internal sealed class ConnectionPool : IConnectionPool
{
	private readonly ConcurrentDictionary<string, Lazy<Task<ConnectionMultiplexer>>> connections;
	private readonly ILogger<ConnectionPool> logger;
	private readonly ConnectionPoolOptions options;
	private bool disposed;

	/// <summary>
	/// Initializes a new instance of the ConnectionPool class
	/// </summary>
	/// <param name="logger">Logger instance for recording connection events and errors</param>
	/// <param name="options">Configuration options for the connection pool</param>
	public ConnectionPool(
		ILogger<ConnectionPool> logger,
		IOptions<ConnectionPoolOptions> options)
	{
		this.logger = logger;
		this.options = options.Value;
		connections = new ConcurrentDictionary<string, Lazy<Task<ConnectionMultiplexer>>>();
	}

	/// <summary>
	/// Gets a connection from the pool or creates a new one if necessary
	/// </summary>
	/// <param name="configuration">Redis configuration options</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A Result containing either a ConnectionMultiplexer or an error</returns>
	public async Task<Result<ConnectionMultiplexer>> GetConnection(
		ConfigurationOptions configuration,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var key = configuration.ToString();

			var lazyConnection = connections.GetOrAdd(
				key,
				_ => new Lazy<Task<ConnectionMultiplexer>>(
					() => CreateConnectionAsync(configuration, cancellationToken)));

			var connection = await lazyConnection.Value;

			if (connection.IsConnected)
			{
				return Result.Success(connection);
			}

			await RemoveConnectionAsync(key);
			return Result.Failure<ConnectionMultiplexer>(DomainErrors.Connection.ConnectionNotEstablished);

		}
		catch (OperationCanceledException)
		{
			return Result.Failure<ConnectionMultiplexer>(
				new Error("ConnectionPool.Cancelled", "Operation was cancelled"));
		}
		catch (RedisConnectionException ex)
		{
			logger.LogError(ex, "Failed to establish Redis connection");
			return Result.Failure<ConnectionMultiplexer>(DomainErrors.Connection.ConnectionNotEstablished);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unexpected error while getting Redis connection");
			return Result.Failure<ConnectionMultiplexer>(
				new Error("ConnectionPool.UnexpectedError", "An unexpected error occurred"));
		}
	}

	/// <summary>
	/// Creates a new Redis connection with the specified configuration
	/// </summary>
	private async Task<ConnectionMultiplexer> CreateConnectionAsync(
		ConfigurationOptions configuration,
		CancellationToken cancellationToken)
	{
		var connection = await ConnectionMultiplexer.ConnectAsync(configuration)
			.WaitAsync(options.ConnectionTimeout, cancellationToken);

		connection.ConnectionFailed += OnConnectionFailed;
		connection.ConnectionRestored += OnConnectionRestored;
		connection.ErrorMessage += OnErrorMessage;

		// Monitor connection state and remove if idle
		_ = Task.Run(async () => 
		{
			while (!disposed && connection.IsConnected)
			{
				await Task.Delay(options.IdleTimeout, cancellationToken);
				if (connection.GetCounters().TotalOutstanding == 0)
				{
					await RemoveConnectionAsync(configuration.ToString());
					break;
				}
			}
		}, cancellationToken);

		return connection;
	}

	/// <summary>
	/// Removes a connection from the pool and disposes it
	/// </summary>
	private async Task RemoveConnectionAsync(string key)
	{
		if (connections.TryRemove(key, out var lazyConnection) && 
			lazyConnection.IsValueCreated)
		{
			var connection = await lazyConnection.Value;
			
			connection.ConnectionFailed -= OnConnectionFailed;
			connection.ConnectionRestored -= OnConnectionRestored;
			connection.ErrorMessage -= OnErrorMessage;
			
			await connection.CloseAsync();
			await connection.DisposeAsync();
		}
	}

	/// <summary>
	/// Handles connection failure events
	/// </summary>
	private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e) =>
		logger.LogWarning(
			e.Exception,
			"Redis connection failed. Endpoint: {Endpoint}, FailureType: {FailureType}",
			e.EndPoint,
			e.FailureType);

	private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e) =>
		logger.LogInformation(
			"Redis connection restored. Endpoint: {Endpoint}",
			e.EndPoint);

	private void OnErrorMessage(object sender, RedisErrorEventArgs e) =>
		logger.LogError(
			"Redis error: {Message}. Endpoint: {Endpoint}",
			e.Message,
			e.EndPoint);

	public async ValueTask DisposeAsync()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;

		foreach (var connection in connections.Values)
		{
			if (connection.IsValueCreated)
			{
				var multiplexer = await connection.Value;
				multiplexer.ConnectionFailed -= OnConnectionFailed;
				multiplexer.ConnectionRestored -= OnConnectionRestored;
				multiplexer.ErrorMessage -= OnErrorMessage;
				await multiplexer.CloseAsync();
				await multiplexer.DisposeAsync();
			}
		}

		connections.Clear();
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;

		foreach (var connection in connections.Values)
		{
			if (connection.IsValueCreated)
			{
				var multiplexer = connection.Value.GetAwaiter().GetResult();
				multiplexer.ConnectionFailed -= OnConnectionFailed;
				multiplexer.ConnectionRestored -= OnConnectionRestored;
				multiplexer.ErrorMessage -= OnErrorMessage;
				multiplexer.Close();
				multiplexer.Dispose();
			}
		}

		connections.Clear();
	}
}
