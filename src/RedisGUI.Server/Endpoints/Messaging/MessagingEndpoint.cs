using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Messaging;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Threading;

namespace RedisGUI.Server.Endpoints.Redis;

public sealed record PublishMessageRequest(string Channel, string Message);

public sealed class MessagingEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routes)
	{
		routes.MapGet("messaging/{connectionId:guid}/stream", async (Guid connectionId, string key, IMediator mediator, CancellationToken ct) =>
			(await mediator.Send<object>(new GetStreamQuery(connectionId, key), ct)).ToHttpResult())
			.RequireAuthorization(Policies.Authenticated);

		routes.MapGet("messaging/{connectionId:guid}/channel", async (Guid connectionId, string channel, IMediator mediator, CancellationToken ct) =>
			(await mediator.Send<object>(new GetChannelQuery(connectionId, channel), ct)).ToHttpResult())
			.RequireAuthorization(Policies.Authenticated);

		routes.MapPost("messaging/{connectionId:guid}/publish", async (Guid connectionId, PublishMessageRequest request, IMediator mediator, CancellationToken ct) =>
			(await mediator.Send<long>(new PublishMessageCommand(connectionId, request.Channel, request.Message), ct))
				.MatchHttp(subscribers => Results.Ok(new { subscribers })))
			.RequireAuthorization(Policies.WriteAccess);
	}
}
