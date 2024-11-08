using System;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.DeleteRedisKey;
using RedisGUI.Application.Redis.Commands.SetRedisValue;
using RedisGUI.Application.Redis.Queries.GetRedisValue;
using RedisGUI.Application.Redis.Queries.GetAllRedisValues;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Endpoints.Redis.Requests;

namespace RedisGUI.Server.Endpoints.Redis;

/// <summary>
/// Contains endpoint definitions for Redis operations
/// </summary>
public static class RedisEndpoints
{
    /// <summary>
    /// Registers all Redis-related endpoints in the HTTP request pipeline
    /// </summary>
    /// <param name="routerBuilder">The router builder to add endpoints to</param>
    /// <returns>The router builder for method chaining</returns>
    public static IEndpointRouteBuilder RegisterRedisEndpoints(this IEndpointRouteBuilder routerBuilder)
    {
        var apiVersionSet = routerBuilder.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1.0))
            .ReportApiVersions()
            .Build();

        var group = routerBuilder.MapGroup("api/v{version:apiVersion}/redis")
            .WithApiVersionSet(apiVersionSet);

        group.MapGet("{connectionId:guid}", async (
            ISender sender,
            Guid connectionId,
            string pattern,
            int? pageSize,
            int? pageNumber) =>
        {
            var query = new GetAllRedisValuesQuery(connectionId, pattern, pageSize, pageNumber);
            return await sender.Send(query).Match(Results.Ok, Results.BadRequest);
        }).WithOpenApi();

        group.MapGet("{connectionId:guid}/{key}", async (
            ISender sender,
            Guid connectionId,
            string key) =>
        {
            var query = new GetRedisValueQuery(connectionId, key);
            return await sender.Send(query).Match(Results.Ok, Results.BadRequest);
        }).WithOpenApi();

        group.MapPost("{connectionId:guid}", async (
            ISender sender,
            Guid connectionId,
            SetRedisValueRequest request) =>
        {
	        var command = new SetRedisValueCommand(
		        connectionId,
		        request.Key,
		        request.Value,
		        request.Ttl);

	        return await sender.Send(command).Match(() => Results.Ok(), Results.BadRequest);

		}).WithOpenApi();

        group.MapDelete("{connectionId:guid}/{key}", async (
            ISender sender,
            Guid connectionId,
            string key) =>
        {
            var command = new DeleteRedisKeyCommand(connectionId, key);
            return await sender.Send(command).Match(
                deleted => deleted ? Results.Ok() : Results.NotFound(),
                Results.BadRequest);
        }).WithOpenApi();

        return routerBuilder;
    }
} 
