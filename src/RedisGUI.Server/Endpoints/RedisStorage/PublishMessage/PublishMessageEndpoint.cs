using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.PublishMessage;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.RedisStorage.PublishMessage;

/// <summary>
/// Endpoint for publishing messages to Redis Pub/Sub channels
/// </summary>
public class PublishMessageEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for publishing messages
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("pubsub/{connectionId:guid}/publish", async (
			IMediator sender,
			Guid connectionId,
			PublishMessageRequest request) =>
		{
			var command = new PublishMessageCommand(
				connectionId,
				request.Channel,
				request.Message);

			return await sender.Send(command).Match(() => Results.Ok(), Results.BadRequest);
		})
		.WithName("PublishMessage")
		.WithSummary("Publish a message to a Redis Pub/Sub channel")
		.WithDescription("Publishes a message to the specified Redis channel");
	}
}

/// <summary>
/// Request for publishing a message
/// </summary>
/// <param name="Channel">The channel name</param>
/// <param name="Message">The message content</param>
public sealed record PublishMessageRequest(string Channel, string Message);
