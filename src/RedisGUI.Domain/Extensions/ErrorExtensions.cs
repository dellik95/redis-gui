using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Extensions;

public static class ErrorExtensions
{
	public static string ToCodeString(this IEnumerable<Error> errors)
	{
		errors ??= [];
		return string.Join(", ", errors.Select(x => x.Code));
	}

	public static string ToFormattedMessage(this IEnumerable<Error> errors)
	{
		errors ??= [];
		return string.Join('\n', errors.Select(x => $"{x.Code}: {x.Message}"));
	}
}