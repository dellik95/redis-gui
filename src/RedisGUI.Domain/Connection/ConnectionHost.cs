

using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;
using RedisGUI.Domain.Primitives;
using System.Text.RegularExpressions;



namespace RedisGUI.Domain.Connection;

/// <summary>
///     Represent connection host name
/// </summary>
public sealed partial class ConnectionHost
{
	/// <summary>
	///     Represent max length of host name
	/// </summary>
	public const int MaxLength = 256;

	/// <summary>
	///     Represent regular expression to validate host name
	/// </summary>
	public static readonly Regex HostRegex =
		HostNameRegex();


	/// <summary>
	///     Create new instance of <see cref="ConnectionHost" />
	/// </summary>
	/// <param name="host">Host</param>
	public ConnectionHost(string host)
	{
		Ensure.NotEmpty(host, Error.NullValue);
		Ensure.Is(() => host.Length < MaxLength, DomainErrors.ConnectionHost.LongerThanAllowed);
		Ensure.Is(() => HostRegex.IsMatch(host), DomainErrors.ConnectionHost.InvalidFormat);

		Value = host;
	}

	/// <summary>
	///     Host name
	/// </summary>
	public string Value { get; init; }

	/// <inheritdoc />
	public override string ToString()
	{
		return Value;
	}

	[GeneratedRegex(@"^(https?:\/\/)?((localhost)|(([a-zA-Z0-9-]{1,63}\.)+[a-zA-Z]{2,63})|((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9]))$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
	private static partial Regex HostNameRegex();
}
