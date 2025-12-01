using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.Redis;

/// <summary>
/// Endpoint for deleting Redis keys by pattern.
/// </summary>
public class DeleteKeysByPatternEndpoint : IEndpoint
{
    /// <summary>
    /// Maps the delete keys by pattern endpoint.
    /// </summary>
    /// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
    public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
    {
        routerBuilder.MapDelete("storage/{connectionId:guid}/keys/pattern/{pattern}",
            async (IMediator sender, Guid connectionId, string pattern) =>
            {
                var command = new DeleteKeysByPatternCommand(connectionId, pattern);
                return await sender.Send(command).Match(Results.Ok, Results.BadRequest);
            })
            .WithName("DeleteKeysByPattern")
            .WithTags("Redis")
            .WithOpenApi();
    }
}
