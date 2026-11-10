#nullable enable
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Mediator
{
	public interface IMediator
	{
		Task<Result> Send(ICommand command, CancellationToken cancellationToken = default);
		Task<Result<TResult>> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
		Task<Result<TResult>> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
		Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
	}
}
