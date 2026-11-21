using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Shared.Mediator;

public sealed class ResultErrorBehavior<TRequest, TResponse> : DispatchWeave.IPipelineBehavior<TRequest, TResponse>
	where TRequest : DispatchWeave.IRequest<TResponse>
{
	public async Task<TResponse> Handle(TRequest request, DispatchWeave.RequestHandlerDelegate<TResponse> next, CancellationToken token)
	{
		if (request is not IResultRequest<TResponse> resultRequest)
			return await next();

		try
		{
			return await next();
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception exception)
		{
			var error = exception switch
			{
				IResultErrorException failure => failure.Error,
				DispatchWeave.HandlerResolutionException resolution => new Error(resolution.Code, resolution.Message),
				_ => new Error("Handler.Exception", exception.Message)
			};
			return resultRequest.Failure(error);
		}
	}
}
