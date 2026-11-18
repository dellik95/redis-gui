using System.IO;

namespace RedisGUI.Application.Redis.Queries.ExportRedisData;

/// <summary>
/// Response for export Redis data operation.
/// </summary>
/// <param name="Data">Exported data content.</param>
/// <param name="FileName">Suggested filename for the export.</param>
/// <param name="ContentType">MIME type of the exported data.</param>
public sealed record ExportRedisDataResponse(
	Stream Data,
	string FileName,
	string ContentType);
