using System.Collections.Generic;

namespace RedisGUI.Application.Redis.Commands.ExecuteTransaction;

/// <summary>
/// Response from executing a Redis transaction.
/// </summary>
/// <param name="Success">Indicates if the transaction was successful.</param>
/// <param name="Results">Results from each operation in the transaction.</param>
/// <param name="OperationsExecuted">Number of operations that were executed.</param>
public sealed record ExecuteTransactionResponse(
	bool Success,
	List<string> Results,
	int OperationsExecuted);
