using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.UnsubscribeFromChannel;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.RedisStorage.UnsubscribeFromChannel;

/// <summary>
/// Endpoint for unsubscribing from Redis Pub/Sub channels
/// </summary>
public class UnsubscribeFromChannelEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for unsubscribing from channels
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapDelete("pubsub/{connectionId:guid}/subscribe/{subscriptionId}", async (
			IMediator sender,
			Guid connectionId,
			string subscriptionId) =>
		{
			var command = new UnsubscribeFromChannelCommand(connectionId, subscriptionId);

			return await sender.Send(command).Match(() => Results.Ok(), Results.BadRequest);
		})
		.WithName("UnsubscribeFromChannel")
		.WithSummary("Unsubscribe from a Redis Pub/Sub channel")
		.WithDescription("Removes the subscription identified by the subscription ID");
	}
}
