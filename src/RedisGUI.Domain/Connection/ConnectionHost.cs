using System.Text.RegularExpressions;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Connection;

public sealed class ConnectionHost
{
	private static readonly Regex HostRegex =
		new(
			pattern: @"^(https?:\/\/)?((localhost)|(([a-zA-Z0-9-]{1,63}\.)+[a-zA-Z]{2,63})|((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9]))$",
			options: RegexOptions.Compiled | RegexOptions.IgnoreCase);


	public const int MaxLength = 256;

	public string Value { get; init; }


	public ConnectionHost(string host)
	{
		Ensure.NotEmpty(host, Error.NullValue);
		Ensure.IsNot(() => host.Length <= MaxLength, DomainErrors.ConnectionHost.LongerThanAllowed);
		Ensure.IsNot(() => HostRegex.IsMatch(host), DomainErrors.ConnectionHost.InvalidFormat);

		Value = host;
	}

	public override string ToString() => Value;

}