using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Connection;

public record ConnectionName
{
	public const int MaxLength = 256;

	public string Value { get; init; }

	public ConnectionName(string name)
	{
		Ensure.NotEmpty(name, Error.NullValue);
		Ensure.IsNot(() => name.Length <= MaxLength, DomainErrors.ConnectionName.LongerThanAllowed);

		Value = name;
	}

	public override string ToString() => Value;
}