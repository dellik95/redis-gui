using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;

namespace RedisGUI.Domain.Connection;

public sealed class ConnectionPort
{
	public int Value { get; init; }

	public ConnectionPort(int port)
	{
		Ensure.IsNot(() => port > 0, DomainErrors.ConnectionPort.LessThanAllowed);

		Value = port;
	}

	public override string ToString() => Value.ToString();
}