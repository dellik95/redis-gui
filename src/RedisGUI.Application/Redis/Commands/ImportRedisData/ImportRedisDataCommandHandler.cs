using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

/// <summary>
/// Handles the command to import Redis data.
/// </summary>
internal sealed class ImportRedisDataCommandHandler
    : ICommandHandler<ImportRedisDataCommand, ImportRedisDataResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    /// <summary>
    /// Initializes a new instance of the ImportRedisDataCommandHandler.
    /// </summary>
    /// <param name="connectionRepository">Repository for Redis connections.</param>
    /// <param name="connectionService">Service for Redis connection operations.</param>
    public ImportRedisDataCommandHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    /// <summary>
    /// Handles the command to import Redis data.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the response or an error.</returns>
    public async Task<Result<ImportRedisDataResponse>> Handle(
        ImportRedisDataCommand request,
        CancellationToken cancellationToken)
    {
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId,
            cancellationToken);

        if (connectionResult.IsFailure)
        {
            return Result.Failure<ImportRedisDataResponse>(connectionResult.Error);
        }

        List<RedisImportItem> items;

        try
        {
            items = request.Format == ImportFormat.Json
                ? ParseJson(request.Data)
                : ParseCsv(request.Data);
        }
        catch (Exception ex)
        {
            return Result.Failure<ImportRedisDataResponse>(
                new Error("Import.ParseError", $"Failed to parse import data: {ex.Message}"));
        }

        var importedCount = 0;
        var skippedCount = 0;
        var failedKeys = new List<string>();

        foreach (var item in items)
        {
            try
            {
                if (!request.Overwrite)
                {
                    var existsResult = await connectionService.GetValue(connectionResult.Value, item.Key);
                    if (existsResult.IsSuccess)
                    {
                        skippedCount++;
                        continue;
                    }
                }

                var ttl = item.TimeToLive.HasValue
                    ? TimeSpan.FromSeconds(item.TimeToLive.Value)
                    : (TimeSpan?)null;

                var setResult = await connectionService.SetValue(
                    connectionResult.Value,
                    item.Key,
                    item.Value,
                    ttl);

                if (setResult.IsSuccess)
                {
                    importedCount++;
                }
                else
                {
                    failedKeys.Add(item.Key);
                }
            }
            catch
            {
                failedKeys.Add(item.Key);
            }
        }

        var response = new ImportRedisDataResponse(
            importedCount,
            skippedCount,
            failedKeys.Count,
            failedKeys);

        return Result.Success(response);
    }

    private static List<RedisImportItem> ParseJson(string data)
    {
        var items = JsonSerializer.Deserialize<List<RedisImportItem>>(data);
        return items ?? new List<RedisImportItem>();
    }

    private static List<RedisImportItem> ParseCsv(string data)
    {
        var items = new List<RedisImportItem>();
        var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1)
        {
            return items;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var fields = ParseCsvLine(line);

            if (fields.Length >= 2)
            {
                var item = new RedisImportItem
                {
                    Key = fields[0],
                    Value = fields.Length > 1 ? fields[1] : string.Empty,
                    Type = fields.Length > 2 ? fields[2] : "String",
                    TimeToLive = fields.Length > 3 && double.TryParse(fields[3], out var ttl) ? ttl : null
                };

                items.Add(item);
            }
        }

        return items;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current);
                current = string.Empty;
            }
            else
            {
                current += c;
            }
        }

        fields.Add(current);
        return fields.ToArray();
    }
}
