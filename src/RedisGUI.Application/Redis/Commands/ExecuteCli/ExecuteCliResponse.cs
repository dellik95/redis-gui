namespace RedisGUI.Application.Redis.Commands.ExecuteCli;

/// <summary>
/// Response returned from CLI command execution
/// </summary>
/// <param name="Output">Command output formatted as string</param>
/// <param name="Success">Whether execution was successful</param>
public sealed record ExecuteCliResponse(
    string Output,
    bool Success = true);
