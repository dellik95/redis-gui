using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents a connection name value object
/// </summary>
public record ConnectionName
{
	/// <summary>
	/// Maximum allowed length for connection name
	/// </summary>
	public const int MaxLength = 256;

	/// <summary>
	/// Creates a new instance of ConnectionName
	/// </summary>
	/// <param name="name">The connection name value</param>
	public ConnectionName(string name)
	{
		Ensure.NotEmpty(name, Error.NullValue);
		Ensure.Is(() => name.Length <= MaxLength, DomainErrors.ConnectionName.LongerThanAllowed);

		Value = name;
	}

	/// <summary>
	/// Gets the connection name value
	/// </summary>
	public string Value { get; init; }

	/// <inheritdoc />
	public override string ToString()
	{
		return Value;
	}
}
