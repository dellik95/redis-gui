using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.GetGroupedRedisKeys;

/// <summary>
/// Handles the retrieval and grouping of Redis keys.
/// </summary>
internal sealed class GetGroupedRedisKeysQueryHandler
    : IQueryHandler<GetGroupedRedisKeysQuery, GetGroupedRedisKeysResponse>
{
    private readonly IRedisConnectionRepository connectionRepository;
    private readonly IConnectionService connectionService;

    public GetGroupedRedisKeysQueryHandler(
        IRedisConnectionRepository connectionRepository,
        IConnectionService connectionService)
    {
        this.connectionRepository = connectionRepository;
        this.connectionService = connectionService;
    }

    public async Task<Result<GetGroupedRedisKeysResponse>> Handle(
        GetGroupedRedisKeysQuery request,
        CancellationToken cancellationToken)
    {
        // Get connection
        var connectionResult = await connectionRepository.GetConnectionByIdAsync(
            request.ConnectionId,
            cancellationToken);

        if (connectionResult.IsFailure)
        {
            return Result.Failure<GetGroupedRedisKeysResponse>(connectionResult.Error);
        }

        var connection = connectionResult.Value;

        // Get all keys
        var keys = await connectionService.GetKeys(connection);
        var keyList = keys.ToList();

        // Filter by pattern if specified
        if (!string.IsNullOrEmpty(request.Pattern) && request.Pattern != "*")
        {
            var pattern = request.Pattern.Replace("*", ".*");
            keyList = keyList.Where(k => System.Text.RegularExpressions.Regex.IsMatch(k, pattern)).ToList();
        }

        // Get key info for each key
        var keyInfoTasks = keyList.Select(async key =>
        {
            var valueResult = await connectionService.GetValue(connection, key);
            if (valueResult.IsFailure)
                return null;

            var value = valueResult.Value;
            var shortName = key.Contains(request.Delimiter)
                ? key.Split(request.Delimiter).Last()
                : key;

            return new RedisKeyInfo
            {
                Key = key,
                ShortName = shortName,
                Type = value.Type,
                Size = value.Value?.Length ?? 0,
                TtlSeconds = value.TimeToLive?.TotalSeconds is double ttl ? (long)ttl : null
            };
        });

        var keyInfos = (await Task.WhenAll(keyInfoTasks))
            .Where(k => k != null)
            .Cast<RedisKeyInfo>()
            .ToList();

        // Group keys
        var groups = request.ViewMode.ToLower() == "tree"
            ? GroupKeysAsTree(keyInfos, request.Delimiter)
            : GroupKeysAsFlat(keyInfos, request.Delimiter);

        var response = new GetGroupedRedisKeysResponse(
            groups,
            keyInfos.Count,
            request.Delimiter,
            request.ViewMode);

        return Result.Success(response);
    }

    private IEnumerable<RedisKeyGroup> GroupKeysAsFlat(List<RedisKeyInfo> keyInfos, string delimiter)
    {
        var groups = keyInfos
            .GroupBy(k =>
            {
                var parts = k.Key.Split(delimiter);
                return parts.Length > 1 ? parts[0] : string.Empty;
            })
            .Select(g => new RedisKeyGroup
            {
                GroupName = g.Key,
                FullPath = g.Key,
                Keys = g.ToList(),
                Children = [],
                KeyCount = g.Count(),
                IsExpanded = false
            })
            .OrderBy(g => g.GroupName)
            .ToList();

        return groups;
    }

    private IEnumerable<RedisKeyGroup> GroupKeysAsTree(List<RedisKeyInfo> keyInfos, string delimiter)
    {
        var root = new Dictionary<string, RedisKeyGroup>();

        foreach (var keyInfo in keyInfos)
        {
            var parts = keyInfo.Key.Split(delimiter);
            
            if (parts.Length == 1)
            {
                // Root level key
                if (!root.ContainsKey(string.Empty))
                {
                    root[string.Empty] = new RedisKeyGroup
                    {
                        GroupName = "Root",
                        FullPath = string.Empty,
                        Keys = new List<RedisKeyInfo>(),
                        Children = new List<RedisKeyGroup>(),
                        KeyCount = 0,
                        IsExpanded = true
                    };
                }
                
                var rootGroup = root[string.Empty];
                var updatedKeys = rootGroup.Keys.Append(keyInfo).ToList();
                root[string.Empty] = rootGroup with { Keys = updatedKeys, KeyCount = rootGroup.KeyCount + 1 };
            }
            else
            {
                // Nested key - build tree structure
                var currentPath = string.Empty;
                Dictionary<string, RedisKeyGroup> currentLevel = root;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var part = parts[i];
                    currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}{delimiter}{part}";

                    if (!currentLevel.ContainsKey(part))
                    {
                        currentLevel[part] = new RedisKeyGroup
                        {
                            GroupName = part,
                            FullPath = currentPath,
                            Keys = new List<RedisKeyInfo>(),
                            Children = new List<RedisKeyGroup>(),
                            KeyCount = 0,
                            IsExpanded = false
                        };
                    }

                    var group = currentLevel[part];
                    currentLevel[part] = group with { KeyCount = group.KeyCount + 1 };

                    // Move to next level (children)
                    var childrenDict = group.Children.ToDictionary(c => c.GroupName, c => c);
                    currentLevel = childrenDict;
                }

                // Add the actual key at the leaf level
                var leafGroup = currentLevel.Values.FirstOrDefault();
                if (leafGroup != null)
                {
                    var updatedKeys = leafGroup.Keys.Append(keyInfo).ToList();
                    currentLevel[leafGroup.GroupName] = leafGroup with { Keys = updatedKeys };
                }
            }
        }

        return root.Values.ToList();
    }
}
