using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.ExecuteTransaction;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Redis;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.RedisStorage.ExecuteTransaction;

/// <summary>
/// Endpoint for executing Redis transactions
/// </summary>
public class ExecuteTransactionEndpoint : IEndpoint
{
	/// <summary>
	/// Maps endpoint for executing transactions
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		routerBuilder.MapPost("transaction/{connectionId:guid}/execute", async (
			IMediator sender,
			Guid connectionId,
			ExecuteTransactionRequest request) =>
		{
			var command = new ExecuteTransactionCommand(connectionId, request.Operations);

			return await sender.Send<ExecuteTransactionResponse>(command)
				.Match(Results.Ok, Results.BadRequest);
		})
		.WithName("ExecuteTransaction")
		.WithSummary("Execute a Redis transaction (MULTI/EXEC)")
		.WithDescription("Executes multiple Redis commands atomically within a transaction");
	}
}

/// <summary>
/// Request for executing a transaction
/// </summary>
/// <param name="Operations">List of operations to execute</param>
public sealed record ExecuteTransactionRequest(List<RedisOperation> Operations);
