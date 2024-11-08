using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Represents a connection port value object
/// </summary>
public sealed class ConnectionPort
{
	/// <summary>
	/// Creates a new instance of ConnectionPort
	/// </summary>
	/// <param name="port">The port number</param>
	public ConnectionPort(int port)
	{
		Ensure.Is(() => port >= 0, DomainErrors.ConnectionPort.LessThanAllowed);

		Value = port;
	}

	/// <summary>
	/// Gets the port number value
	/// </summary>
	public int Value { get; init; }

	/// <inheritdoc />
	public override string ToString()
	{
		return Value.ToString();
	}
}
