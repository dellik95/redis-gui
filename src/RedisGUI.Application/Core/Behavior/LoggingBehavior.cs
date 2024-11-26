using MediatR;
using Microsoft.Extensions.Logging;
using RedisGUI.Application.Abstraction.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Behavior;

/// <summary>
/// Mediator pipeline behavior that adds logging around command execution
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public class LoggingBehavior<TRequest, TResponse>
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IBaseCommand
{
	private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;

	/// <summary>
	/// Initializes a new instance of the LoggingBehavior class
	/// </summary>
	/// <param name="logger">The logger instance for this behavior</param>
	public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
	{
		this.logger = logger;
	}

	/// <summary>
	/// Handles the request by adding logging before and after command execution
	/// </summary>
	/// <param name="request">The request being handled</param>
	/// <param name="next">The delegate for the next action in the pipeline</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The response from the handler</returns>
	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		var name = request.GetType().Name;

		try
		{
			logger.LogInformation("Starting executing command {Command}", name);
			var result = await next();

			logger.LogInformation("Command {Command} processed successfully.", name);

			return result;
		}
		catch (Exception e)
		{
			logger.LogError(e, "Command {Command} processing failed.", name);

			throw;
		}
	}
}
