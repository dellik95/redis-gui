using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Queries.ExportRedisData;

/// <summary>
/// Handles the query to export Redis data.
/// </summary>
internal sealed class ExportRedisDataQueryHandler
    : IQueryHandler<ExportRedisDataQuery, ExportRedisDataResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    /// <summary>
    /// Initializes a new instance of the ExportRedisDataQueryHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="connectionService">Service for Redis connection operations.</param>
    public ExportRedisDataQueryHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    /// <summary>
    /// Handles the query to export Redis data.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the response or an error.</returns>
    public async Task<Result<ExportRedisDataResponse>> Handle(
        ExportRedisDataQuery request,
        CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId,
            cancellationToken);

        if (connectionResult.IsFailure)
        {
            return Result.Failure<ExportRedisDataResponse>(connectionResult.Error);
        }

        var valuesResult = await connectionService.GetAllValues(
            connectionResult.Value,
            request.Pattern);

        if (valuesResult.IsFailure)
        {
            return Result.Failure<ExportRedisDataResponse>(valuesResult.Error);
        }

        var pagedValues = valuesResult.Value;
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        
        string data;
        string fileName;
        string contentType;

        if (request.Format == ExportFormat.Json)
        {
            var exportData = pagedValues.Values.Select(v => new
            {
                Key = v.Key,
                Value = v.Value,
                Type = v.Type,
                TimeToLive = v.TimeToLive?.TotalSeconds
            });

            data = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            fileName = $"redis_export_{timestamp}.json";
            contentType = "application/json";
        }
        else
        {
            var csv = new StringBuilder();
            csv.AppendLine("Key,Value,Type,TTL");

            foreach (var value in pagedValues.Values)
            {
                var ttl = value.TimeToLive?.TotalSeconds.ToString() ?? "";
                var escapedValue = value.Value?.Replace("\"", "\"\"") ?? "";
                csv.AppendLine($"\"{value.Key}\",\"{escapedValue}\",\"{value.Type}\",\"{ttl}\"");
            }

            data = csv.ToString();
            fileName = $"redis_export_{timestamp}.csv";
            contentType = "text/csv";
        }

        var response = new ExportRedisDataResponse(data, fileName, contentType);
        return Result.Success(response);
    }
}
