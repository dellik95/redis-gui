using MediatR;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Abstraction.Messaging;

/// <summary>
/// Represents a query that returns a response of type TResponse
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
