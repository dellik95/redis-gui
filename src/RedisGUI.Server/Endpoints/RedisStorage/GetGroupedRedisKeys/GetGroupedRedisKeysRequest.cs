using System;

namespace RedisGUI.Server.Endpoints.RedisStorage.GetGroupedRedisKeys;

/// <summary>
/// Request to get grouped Redis keys
/// </summary>
public sealed record GetGroupedRedisKeysRequest(
	Guid ConnectionId,
	string Pattern = "*",
	string Delimiter = ":",
	string ViewMode = "tree");
