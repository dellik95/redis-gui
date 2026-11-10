using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Application.Redis.Commands.SeedSampleData;

/// <summary>
/// Command to populate Redis with rich configurable demo/sample data across multiple databases
/// </summary>
public sealed record SeedSampleDataCommand(
	Guid ConnectionId,
	SeedSampleDataOptions? Options = null) : ICommand<SeedDataResult>;
