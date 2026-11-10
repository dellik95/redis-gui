using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.ExportRedisData;

/// <summary>
/// Handles the query to export Redis data.
/// </summary>
internal sealed class ExportRedisDataQueryHandler
	: IQueryHandler<ExportRedisDataQuery, ExportRedisDataResponse>
{
	private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IRedisValueService valueService;

	/// <summary>
	/// Initializes a new instance of the ExportRedisDataQueryHandler.
	/// </summary>
	/// <param name="connectionRepository">Repository for Redis connections.</param>
	/// <param name="valueService">Service for Redis value operations.</param>
	public ExportRedisDataQueryHandler(
		IRedisConnectionRepository connectionRepository,
		IRedisValueService valueService)
	{
		this.connectionRepository = connectionRepository;
		this.valueService = valueService;
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
		Result<RedisConnection> connectionResult = await connectionRepository.GetConnectionByIdAsync(
			request.ConnectionId,
			cancellationToken);

		if (connectionResult.IsFailure)
		{
			return Result.Failure<ExportRedisDataResponse>(connectionResult.Error);
		}

		Result<PagedRedisValues> valuesResult = await valueService.GetAllValuesAsync(
			connectionResult.Value,
			request.Pattern,
			cancellationToken: cancellationToken);

		if (valuesResult.IsFailure)
		{
			return Result.Failure<ExportRedisDataResponse>(valuesResult.Error);
		}

		PagedRedisValues pagedValues = valuesResult.Value;
		string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

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

			// Cached JsonSerializerOptions to avoid creating new instances
			data = JsonSerializer.Serialize(exportData, jsonOptions);
			fileName = $"redis_export_{timestamp}.json";
			contentType = "application/json";
		}
		else
		{
			StringBuilder csv = new();
			csv.AppendLine("Key,Value,Type,TTL");

			foreach (RedisValue value in pagedValues.Values)
			{
				string ttl = value.TimeToLive?.TotalSeconds.ToString() ?? "";
				string escapedValue = value.Value?.Replace("\"", "\"\"") ?? "";
				csv.AppendLine($"\"{value.Key}\",\"{escapedValue}\",\"{value.Type}\",\"{ttl}\"");
			}

			data = csv.ToString();
			fileName = $"redis_export_{timestamp}.csv";
			contentType = "text/csv";
		}

		ExportRedisDataResponse response = new(data, fileName, contentType);
		return Result.Success(response);
	}
}
