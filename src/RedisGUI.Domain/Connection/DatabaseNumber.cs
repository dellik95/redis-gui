using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents a Redis database number value object
/// </summary>
public sealed class DatabaseNumber : ValueObject
{
	/// <summary>
	/// Maximum allowed database number (Redis typically supports 0-15, but can be configured for more)
	/// </summary>
	public const int MaxValue = 65535;

	/// <summary>
	/// Creates a new instance of DatabaseNumber
	/// </summary>
	/// <param name="databaseNumber">The database number</param>
	public DatabaseNumber(int databaseNumber)
	{
		Ensure.Is(() => databaseNumber >= 0, DomainErrors.DatabaseNumber.LessThanAllowed);
		Ensure.Is(() => databaseNumber <= MaxValue, DomainErrors.DatabaseNumber.GreaterThanAllowed);

		Value = databaseNumber;
	}

	/// <summary>
	/// Gets the database number value
	/// </summary>
	public int Value { get; }

	/// <inheritdoc />
	public override string ToString()
	{
		return Value.ToString();
	}

	/// <inheritdoc />
	protected override IEnumerable<object> GetAtomicValues()
	{
		yield return Value;
	}
}

