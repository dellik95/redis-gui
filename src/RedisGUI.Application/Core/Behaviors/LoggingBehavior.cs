using Microsoft.Extensions.Logging;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Behaviors;

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

			if (result is Result { IsFailure: true } failure)
			{
				logger.LogWarning("Request {Request} failed with {ErrorCode}.", name, failure.Error.Code);
			}
			else
			{
				logger.LogInformation("Request {Request} processed successfully.", name);
			}

			return result;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			logger.LogInformation("Request {Request} was cancelled.", name);
			throw;
		}
		catch (Exception e)
		{
			logger.LogError(e, "Request {Request} processing failed.", name);

			throw;
		}
	}
}
