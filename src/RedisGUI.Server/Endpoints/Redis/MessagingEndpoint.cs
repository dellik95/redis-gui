using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Domain.Connection;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.Redis.Services;
using RedisGUI.Server.Abstraction;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Server.Endpoints.Redis;

public sealed record PublishMessageRequest(string Channel, string Message);

public sealed class MessagingEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routes)
	{
		routes.MapGet("messaging/{connectionId:guid}/stream", async (Guid connectionId, string key, IRedisConnectionRepository repo, IAuthorizationService auth, HttpContext context, RedisMessagingService service, CancellationToken ct) =>
		{
			if (string.IsNullOrWhiteSpace(key) || key.Length > 512) return new Error("Messaging.InvalidStream", "Enter a stream key.").ToProblemDetails();
			var found = await repo.GetConnectionByIdAsync(connectionId, ct);
			if (found.IsFailure) return found.Error.ToProblemDetails();
			if (!(await auth.AuthorizeAsync(context.User, found.Value, new ConnectionOwnershipRequirement())).Succeeded) return new Error("Connection.Forbidden", "Access to the connection is denied.").ToProblemDetails();
			return Results.Ok(await service.StreamAsync(found.Value, key, ct));
		}).RequireAuthorization(Policies.Authenticated);

		routes.MapGet("messaging/{connectionId:guid}/channel", async (Guid connectionId, string channel, IRedisConnectionRepository repo, IAuthorizationService auth, HttpContext context, RedisMessagingMetricsCollector collector, CancellationToken ct) =>
		{
			if (string.IsNullOrWhiteSpace(channel) || channel.Length > 512) return new Error("Messaging.InvalidChannel", "Enter a channel.").ToProblemDetails();
			var found = await repo.GetConnectionByIdAsync(connectionId, ct);
			if (found.IsFailure) return found.Error.ToProblemDetails();
			if (!(await auth.AuthorizeAsync(context.User, found.Value, new ConnectionOwnershipRequirement())).Succeeded) return new Error("Connection.Forbidden", "Access to the connection is denied.").ToProblemDetails();
			return Results.Ok(await collector.CollectAsync(found.Value, channel, ct));
		}).RequireAuthorization(Policies.Authenticated);

		routes.MapPost("messaging/{connectionId:guid}/publish", async (Guid connectionId, PublishMessageRequest request, IRedisConnectionRepository repo, IAuthorizationService auth, HttpContext context, RedisMessagingService service, CancellationToken ct) =>
		{
			if (string.IsNullOrWhiteSpace(request.Channel) || request.Channel.Length > 512 || request.Message is null || request.Message.Length > 1048576) return new Error("Messaging.InvalidMessage", "Invalid channel or message.").ToProblemDetails();
			var found = await repo.GetConnectionByIdAsync(connectionId, ct);
			if (found.IsFailure) return found.Error.ToProblemDetails();
			if (!(await auth.AuthorizeAsync(context.User, found.Value, new ConnectionOwnershipRequirement())).Succeeded || found.Value.IsReadOnly) return new Error("Connection.Forbidden", "Access to the connection is denied.").ToProblemDetails();
			return Results.Ok(new { subscribers = await service.PublishAsync(found.Value, request.Channel, request.Message, ct) });
		}).RequireAuthorization(Policies.WriteAccess);
	}
}
