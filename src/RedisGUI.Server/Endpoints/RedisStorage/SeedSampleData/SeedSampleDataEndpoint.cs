using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Redis.Commands.SeedSampleData;
using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Redis;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisGUI.Server.Endpoints.RedisStorage.SeedSampleData;

public class SeedSampleDataEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder routerBuilder)
	{
		async Task<IResult> HandleSeed(
			IMediator sender,
			Guid connectionId,
			SeedSampleDataRequest? request)
		{
			SeedSampleDataOptions? options = null;
			if (request != null)
			{
				options = new SeedSampleDataOptions
				{
					TotalKeys = request.KeyCount ?? request.TotalKeys,
					Databases = request.Databases ?? request.TargetDatabases,
					RandomizeDatabases = request.DistributeRandomlyAcrossDatabases ?? request.RandomizeDatabases,
					RandomDatabasesCount = request.RandomDbCount ?? request.RandomDatabasesCount,
					DataTypes = request.DataTypes,
					KeyComplexity = request.KeyComplexity,
					PayloadComplexity = request.PayloadComplexity,
					TtlStrategy = request.TtlStrategy,
					CustomPrefix = request.CustomPrefix ?? request.KeyPrefix
				};
			}

			var command = new SeedSampleDataCommand(connectionId, options);
			return await sender.Send<SeedDataResult>(command).Match(
				Results.Ok,
				Results.BadRequest);
		}

		routerBuilder.MapPost("storage/{connectionId:guid}/seed-sample-data", HandleSeed)
			.WithName("SeedSampleData")
			.WithSummary("Seed configurable sample Redis data")
			.WithDescription("Populates Redis with rich customizable demo data across databases with configurable types, key structures, and TTLs")
			.WithTags("Redis Storage")
			.Produces<SeedDataResult>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest);

		routerBuilder.MapPost("storage/{connectionId:guid}/seed", HandleSeed)
			.WithName("SeedSampleDataLegacy")
			.WithSummary("Seed sample Redis data (legacy alias)")
			.WithTags("Redis Storage")
			.Produces<SeedDataResult>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest);
	}
}

public class SeedSampleDataRequest
{
	public int TotalKeys { get; set; } = 50;
	public int? KeyCount { get => TotalKeys; set => TotalKeys = value ?? TotalKeys; }

	public List<int>? Databases { get; set; }
	public List<int>? TargetDatabases { get => Databases; set => Databases = value; }

	public bool RandomizeDatabases { get; set; } = false;
	public bool? DistributeRandomlyAcrossDatabases { get => RandomizeDatabases; set => RandomizeDatabases = value ?? RandomizeDatabases; }

	public int RandomDatabasesCount { get; set; } = 3;
	public int? RandomDbCount { get => RandomDatabasesCount; set => RandomDatabasesCount = value ?? RandomDatabasesCount; }

	public List<string>? DataTypes { get; set; }
	public string KeyComplexity { get; set; } = "hierarchical";
	public string PayloadComplexity { get; set; } = "json";
	public string TtlStrategy { get; set; } = "mixed";
	public string? CustomPrefix { get; set; } = "demo";
	public string? KeyPrefix { get => CustomPrefix; set => CustomPrefix = value; }
}
