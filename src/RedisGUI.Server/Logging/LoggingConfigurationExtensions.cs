using Microsoft.Extensions.Logging;

namespace RedisGUI.Server.Logging;

/// <summary>
/// Configures structured logging for the application.
/// </summary>
public static class LoggingConfigurationExtensions
{
	public static ILoggingBuilder ConfigureApplicationLogging(this ILoggingBuilder builder)
	{
		// Use simple, structured console logging by default.
		builder.ClearProviders();
		builder.AddSimpleConsole(options =>
		{
			options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz ";
			options.IncludeScopes = true;
		});

		return builder;
	}
}


