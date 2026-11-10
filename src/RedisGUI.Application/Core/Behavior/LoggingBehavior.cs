using Microsoft.Extensions.Logging;
using RedisGUI.Shared.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Behavior;

public class LoggingBehavior<TRequest, TResponse>
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;

	public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
	{
		this.logger = logger;
	}

	public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>> next)
	{
		var name = request.GetType().Name;

		try
		{
			logger.LogInformation("Starting executing request {Request}", name);
			var result = await next();

			logger.LogInformation("Request {Request} processed successfully.", name);

			return result;
		}
		catch (Exception e)
		{
			logger.LogError(e, "Request {Request} processing failed.", name);

			throw;
		}
	}
}
