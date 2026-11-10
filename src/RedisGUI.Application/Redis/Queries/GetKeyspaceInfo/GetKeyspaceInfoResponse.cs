using System.Collections.Generic;

namespace RedisGUI.Application.Redis.Queries.GetKeyspaceInfo;

/// <summary>
/// Response containing key counts per database
/// </summary>
/// <param name="Databases">Dictionary mapping database index (0-15) to total keys count</param>
public sealed record GetKeyspaceInfoResponse(IReadOnlyDictionary<int, long> Databases);
