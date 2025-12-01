using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Queries.ExportRedisData;
using RedisGUI.Domain.Extensions;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Server.Endpoints.Redis;

/// <summary>
/// Endpoint for exporting Redis data.
/// </summary>
public class ExportRedisDataEndpoint : IEndpoint
{
    /// <summary>
    /// Maps the export Redis data endpoint.
    /// </summary>
    /// <param name="routerBuilder">The router builder used to configure the endpoint.</param>
    public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
    {
        routerBuilder.MapGet("storage/{connectionId:guid}/export",
            async (IMediator sender, Guid connectionId, string pattern, string format) =>
            {
                var exportFormat = format?.ToLower() == "csv" ? ExportFormat.Csv : ExportFormat.Json;
                var query = new ExportRedisDataQuery(connectionId, pattern ?? "*", exportFormat);
                
                return await sender.Send(query).Match(
                    success => Results.File(
                        Encoding.UTF8.GetBytes(success.Data),
                        success.ContentType,
                        success.FileName),
                    error => Results.BadRequest(error));
            })
            .WithName("ExportRedisData")
            .WithTags("Redis")
            .WithOpenApi();
    }
}
