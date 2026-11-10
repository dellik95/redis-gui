using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.Redis;

/// <summary>
/// Endpoint for deleting multiple Redis keys.
/// </summary>
public class DeleteMultipleKeysEndpoint : IEndpoint
{
    /// <summary>
    /// Maps the delete multiple keys endpoint.
    /// </summary>
    /// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
    public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
    {
        routerBuilder.MapPost("storage/{connectionId:guid}/keys/delete-multiple",
            async (IMediator sender, Guid connectionId, DeleteKeysRequest request, int? database) =>
            {
                var command = new DeleteMultipleKeysCommand(connectionId, request.Keys, database ?? request.Database);
                return await sender.Send(command).Match(Results.Ok, Results.BadRequest);
            })
            .WithName("DeleteMultipleKeys")
            .WithTags("Redis")
            .WithOpenApi();
    }

    /// <summary>
    /// Request model for deleting multiple keys.
    /// </summary>
    /// <param name="Keys">List of keys to delete.</param>
    /// <param name="Database">Optional database number.</param>
    public sealed record DeleteKeysRequest(string[] Keys, int? Database = null);
}
