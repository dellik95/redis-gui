namespace RedisGUI.Server.Endpoints.RedisStorage.RunLoadTest;

public sealed record RunLoadTestRequest(
	int OperationsCount = 1000,
	int Concurrency = 5,
	string OperationType = "mixed",
	int PayloadSizeBytes = 128,
	int? Database = null,
	string? KeyPrefix = null,
	int DurationSeconds = 0);
