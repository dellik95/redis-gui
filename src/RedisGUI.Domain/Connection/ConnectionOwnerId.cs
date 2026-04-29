using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents an optional connection owner ID value object
/// </summary>
public sealed class ConnectionOwnerId : ValueObject
{
	/// <summary>
	/// Creates a new instance of ConnectionOwnerId with a specific owner ID
	/// </summary>
	/// <param name="ownerId">The owner's user ID</param>
	public ConnectionOwnerId(Guid ownerId)
	{
		Ensure.Is(() => ownerId != Guid.Empty, DomainErrors.ConnectionOwnerId.Empty);
		Value = ownerId;
	}

	/// <summary>
	/// Private constructor for None (public connection)
	/// </summary>
	private ConnectionOwnerId()
	{
		Value = null;
	}

	/// <summary>
	/// Gets the owner ID value (null if public/shared connection)
	/// </summary>
	public Guid? Value { get; }

	/// <summary>
	/// Gets whether this connection has an owner
	/// </summary>
	public bool HasValue => Value.HasValue;

	/// <summary>
	/// Creates a ConnectionOwnerId with a specific owner
	/// </summary>
	/// <param name="ownerId">The owner's user ID</param>
	/// <returns>A ConnectionOwnerId with the specified owner</returns>
	public static ConnectionOwnerId Some(Guid ownerId)
	{
		return new ConnectionOwnerId(ownerId);
	}

	/// <summary>
	/// Creates a ConnectionOwnerId representing a public/shared connection (no owner)
	/// </summary>
	/// <returns>A ConnectionOwnerId with no owner</returns>
	public static ConnectionOwnerId None()
	{
		return new ConnectionOwnerId();
	}

	/// <summary>
	/// Creates a ConnectionOwnerId from an optional Guid
	/// </summary>
	/// <param name="ownerId">The optional owner ID</param>
	/// <returns>A ConnectionOwnerId instance</returns>
	public static ConnectionOwnerId FromNullable(Guid? ownerId)
	{
		return ownerId.HasValue ? Some(ownerId.Value) : None();
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return Value?.ToString() ?? "None";
	}

	/// <inheritdoc />
	protected override IEnumerable<object?> GetAtomicValues()
	{
		yield return Value;
	}
}

