using FluentResults;
using System;
using System.Text;
using RedisGUI.Application.Core.Services;
using DispatchWeave;
using DispatchWeave.Results;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.GetRedisValue;

/// <summary>
/// Query to retrieve a single Redis value by key.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Key">The key to retrieve.</param>
/// <param name="Database">Optional database index.</param>
/// <param name="KeyBase64">Optional Base64-encoded raw Redis key bytes.</param>
public sealed record GetRedisValueQuery(Guid ConnectionId, string Key, int? Database = null, string? KeyBase64 = null) : IQuery<GetRedisValueResponse>;

/// <summary>
/// Handles the query to retrieve a single Redis value.
/// </summary>
internal sealed class GetRedisValueQueryHandler(
	IRedisConnectionRepository connectionRepository,
	IRedisValueService valueService,
	IConnectionAccessService? access = null) : IQueryHandler<GetRedisValueQuery, GetRedisValueResponse>
{
	private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public async Task<Result<GetRedisValueResponse>> Handle(GetRedisValueQuery request, CancellationToken cancellationToken)
	{
		var connectionResult = await connectionRepository.GetAuthorizedConnectionAsync(request.ConnectionId, access, cancellationToken);

		if (connectionResult.IsFailed)
		{
			return Result.Fail<GetRedisValueResponse>(connectionResult.Errors);
		}

		var (displayKey, keyBase64, isBinary) = DescribeIdentity(request.Key, request.KeyBase64);

		var result = string.IsNullOrEmpty(request.KeyBase64)
			? await valueService.GetValueAsync(
				connectionResult.Value,
				displayKey,
				cancellationToken,
				request.Database)
			: await valueService.GetValueByIdentityAsync(
				connectionResult.Value,
				displayKey,
				request.KeyBase64,
				cancellationToken,
				request.Database);

		if (result.IsFailed)
		{
			return Result.Fail<GetRedisValueResponse>(result.Errors);
		}

		object parsedValue;
		string rawValue = result.Value.Value;

		if (string.Equals(result.Value.Type, "string", StringComparison.OrdinalIgnoreCase))
		{
			parsedValue = rawValue;
		}
		else
		{
			try
			{
				parsedValue = JsonSerializer.Deserialize<JsonElement>(rawValue);
			}
			catch
			{
				parsedValue = rawValue;
			}
		}

		long? ttlSeconds = result.Value.TimeToLive.HasValue
			? (long)result.Value.TimeToLive.Value.TotalSeconds
			: null;

		return Result.Ok(new GetRedisValueResponse(
			displayKey,
			parsedValue,
			result.Value.Type,
			result.Value.TimeToLive,
			ttlSeconds,
			keyBase64,
			isBinary));
	}

	private static (string Key, string? KeyBase64, bool IsBinary) DescribeIdentity(string key, string? keyBase64)
	{
		if (!string.IsNullOrEmpty(keyBase64))
		{
			try
			{
				byte[] raw = Convert.FromBase64String(keyBase64);
				try
				{
					string decoded = StrictUtf8.GetString(raw);
					return (decoded, keyBase64, false);
				}
				catch (DecoderFallbackException)
				{
					string fallback = !string.IsNullOrEmpty(key) ? key : Encoding.UTF8.GetString(raw);
					return (fallback, keyBase64, true);
				}
			}
			catch (FormatException)
			{
				return (key, keyBase64, false);
			}
		}

		return string.IsNullOrEmpty(key)
			? (key, null, false)
			: (key, Convert.ToBase64String(Encoding.UTF8.GetBytes(key)), false);
	}
}
