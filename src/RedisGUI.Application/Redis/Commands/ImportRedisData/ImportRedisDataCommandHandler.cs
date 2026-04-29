using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

/// <summary>
/// Handles the command to import Redis data.
/// </summary>
internal sealed class ImportRedisDataCommandHandler
	: ICommandHandler<ImportRedisDataCommand, ImportRedisDataResponse>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisValueService valueService;

	/// <summary>
	/// Initializes a new instance of the ImportRedisDataCommandHandler.
	/// </summary>
	/// <param name="connectionRepository">Repository for Redis connections.</param>
	/// <param name="valueService">Service for Redis value operations.</param>
	public ImportRedisDataCommandHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisValueService valueService)
	{
		this.connectionRepository = connectionRepository;
		this.valueService = valueService;
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
		Result<RedisConnection> connectionResult = await connectionRepository.GetConnectionByIdAsync(
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
		catch (Exception)
		{
			return Result.Failure<ImportRedisDataResponse>(
				DomainErrors.Import.ParseError);
		}

		int importedCount = 0;
		int skippedCount = 0;
		List<string> failedKeys = new();

		foreach (RedisImportItem item in items)
		{
			try
			{
				if (!request.Overwrite)
				{
					Result<RedisValue> existsResult = await valueService.GetValueAsync(connectionResult.Value, item.Key, cancellationToken);
					if (existsResult.IsSuccess)
					{
						skippedCount++;
						continue;
					}
				}

				TimeSpan? ttl = item.TimeToLive.HasValue
					? TimeSpan.FromSeconds(item.TimeToLive.Value)
					: null;

				Result setResult = await valueService.SetValueAsync(
					connectionResult.Value,
					item.Key,
					item.Value,
					ttl,
					cancellationToken);

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

		ImportRedisDataResponse response = new(
			importedCount,
			skippedCount,
			failedKeys.Count,
			failedKeys);

		return Result.Success(response);
	}

	private static List<RedisImportItem> ParseJson(string data)
	{
		List<RedisImportItem>? items = JsonSerializer.Deserialize<List<RedisImportItem>>(data);
		return items ?? [];
	}

	private static List<RedisImportItem> ParseCsv(string data)
	{
		List<RedisImportItem> items = new();
		string[] lines = data.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length <= 1)
		{
			return items;
		}

		for (int i = 1; i < lines.Length; i++)
		{
			string line = lines[i];
			string[] fields = ParseCsvLine(line);

			if (fields.Length >= 2)
			{
				RedisImportItem item = new()
				{
					Key = fields[0],
					Value = fields.Length > 1 ? fields[1] : string.Empty,
					Type = fields.Length > 2 ? fields[2] : "String",
					TimeToLive = fields.Length > 3 && double.TryParse(fields[3], out double ttl) ? ttl : null
				};

				items.Add(item);
			}
		}

		return items;
	}

	private static string[] ParseCsvLine(string line)
	{
		List<string> fields = new();
		string current = string.Empty;
		bool inQuotes = false;

		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];

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
