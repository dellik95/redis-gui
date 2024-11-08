using MediatR;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Abstraction.Messaging;

/// <summary>
/// Contract for Query handler that processes queries and returns responses
/// </summary>
/// <typeparam name="TQuery">The type of query to handle</typeparam>
/// <typeparam name="TResponse">The type of response returned by the query</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
