namespace RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;

/// <summary>
/// Response for bulk delete by pattern operation.
/// </summary>
/// <param name="DeletedCount">Number of keys deleted.</param>
/// <param name="Pattern">Pattern used for deletion.</param>
public sealed record DeleteKeysByPatternResponse(
	int DeletedCount,
	string Pattern);
