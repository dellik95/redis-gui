using System.Collections.Generic;
using RedisGUI.Domain.Audit;

namespace RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;

/// <summary>
/// Response for bulk delete multiple keys operation.
/// </summary>
/// <param name="DeletedCount">Number of keys successfully deleted.</param>
/// <param name="TotalRequested">Total number of keys requested for deletion.</param>
/// <param name="FailedKeys">List of keys that failed to delete.</param>
public sealed record DeleteMultipleKeysResponse(
	int DeletedCount,
	int TotalRequested,
	IReadOnlyList<string> FailedKeys) : IAuditOutcome
{
	public string FormatAuditOutcome() =>
		$"Deleted {DeletedCount} of {TotalRequested}; failed or missing: {FailedKeys.Count}.";
}
