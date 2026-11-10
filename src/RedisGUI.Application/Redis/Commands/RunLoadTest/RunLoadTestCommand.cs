using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Application.Redis.Commands.RunLoadTest;

/// <summary>
/// Command to run a synthetic load test against Redis with guaranteed test key cleanup
/// </summary>
public sealed record RunLoadTestCommand(
	Guid ConnectionId,
	int OperationsCount = 1000,
	int Concurrency = 5,
	string OperationType = "mixed",
	int PayloadSizeBytes = 128,
	int? Database = null,
	string? KeyPrefix = null,
	int DurationSeconds = 0) : ICommand<RedisLoadTestResult>;
