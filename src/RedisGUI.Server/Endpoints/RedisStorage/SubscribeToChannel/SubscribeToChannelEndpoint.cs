using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.SubscribeToChannel;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.RedisStorage.SubscribeToChannel;

/// <summary>
/// Endpoint for subscribing to Redis Pub/Sub channels
/// </summary>
public class SubscribeToChannelEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for subscribing to channels
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("pubsub/{connectionId:guid}/subscribe/{channel}", async (
			IMediator sender,
			Guid connectionId,
			string channel) =>
		{
			var query = new SubscribeToChannelQuery(connectionId, channel);

			return await sender.Send<SubscribeToChannelResponse>(query)
				.Match(Results.Ok, Results.BadRequest);
		})
		.WithName("SubscribeToChannel")
		.WithSummary("Subscribe to a Redis Pub/Sub channel")
		.WithDescription("Creates a subscription to the specified Redis channel");
	}
}
