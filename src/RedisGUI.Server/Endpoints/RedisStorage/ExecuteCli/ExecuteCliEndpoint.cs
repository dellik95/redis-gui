using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.ExecuteCli;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.RedisStorage.ExecuteCli;

/// <summary>
/// Endpoint for executing raw Redis commands via CLI terminal
/// </summary>
public sealed class ExecuteCliEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
    {
        routerBuilder.MapPost("storage/{connectionId:guid}/cli", async (
            IMediator sender,
            Guid connectionId,
            ExecuteCliRequest request,
            CancellationToken cancellationToken) =>
        {
            ExecuteCliCommand command = new(connectionId, request.Command, request.Database);
            return await sender.Send<ExecuteCliResponse>(command, cancellationToken)
                .Match(Results.Ok, Results.BadRequest);
        })
        .WithName("ExecuteRedisCli")
        .WithSummary("Execute raw Redis CLI command")
        .WithDescription("Executes an arbitrary Redis command in the context of the connection and returns formatted terminal output")
        .WithTags("Redis Storage")
        .Produces<ExecuteCliResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(Policies.Authenticated);
    }
}
