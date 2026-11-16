namespace RedisGUI.Server.Endpoints.RedisStorage.ExecuteCli;

/// <summary>
/// Request model for executing a raw CLI command
/// </summary>
/// <param name="Command">The Redis command to execute</param>
/// <param name="Database">Optional database index</param>
public sealed record ExecuteCliRequest(string Command, int? Database = null);
