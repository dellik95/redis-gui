using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using System;

namespace RedisGUI.Application.Redis.Queries.ScanRedisKeys;

public sealed record ScanRedisKeysQuery(Guid ConnectionId, string Pattern, int? Database, string? Cursor, int Count) : IQuery<RedisKeyScanPage>;
