using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RedisGUI.Infrastructure.Persistence.Cache;

namespace RedisGUI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Intercepts database commands to provide caching functionality
/// </summary>
public class CachingInterceptor : DbCommandInterceptor
{
	/// <summary>
	/// The processor that handles the actual caching logic
	/// </summary>
	private readonly IDbCommandInterceptorProcessor processor;

	/// <summary>
	/// Initializes a new instance of the CachingInterceptor
	/// </summary>
	/// <param name="processor">The processor that handles caching operations</param>
	/// <exception cref="ArgumentNullException">Thrown when processor is null</exception>
	public CachingInterceptor(IDbCommandInterceptorProcessor processor)
	{
		this.processor = processor ?? throw new ArgumentNullException(nameof(processor));
	}

	/// <inheritdoc />
	public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
	{
		return processor.ProcessExecutedCommands(command, eventData.Context, result).GetAwaiter().GetResult();
	}

	/// <inheritdoc />
	public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
	{
		return await processor.ProcessExecutedCommands(command, eventData.Context, result, cancellationToken);
	}

	/// <inheritdoc />
	public override InterceptionResult<int> NonQueryExecuting(DbCommand command,
		CommandEventData eventData,
		InterceptionResult<int> result)
	{
		return processor.ProcessExecutingCommands(command, eventData.Context, result).GetAwaiter().GetResult();
	}

	/// <inheritdoc />
	public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
		CommandEventData eventData,
		InterceptionResult<int> result,
		CancellationToken cancellationToken = default)

	{
		return await processor.ProcessExecutingCommands(command, eventData.Context, result, cancellationToken);
	}

	/// <inheritdoc />
	public override DbDataReader ReaderExecuted(DbCommand command,
		CommandExecutedEventData eventData,
		DbDataReader result)
	{
		return processor.ProcessExecutedCommands(command, eventData.Context, result).GetAwaiter().GetResult();
	}

	/// <inheritdoc />
	public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
		CommandExecutedEventData eventData,
		DbDataReader result,
		CancellationToken cancellationToken = default)
	{
		return await processor.ProcessExecutedCommands(command, eventData.Context, result, cancellationToken);
	}

	/// <inheritdoc />
	public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,
		CommandEventData eventData,
		InterceptionResult<DbDataReader> result)
	{
		return processor.ProcessExecutingCommands(command, eventData.Context, result).GetAwaiter().GetResult();
	}

	/// <inheritdoc />
	public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
		CommandEventData eventData,
		InterceptionResult<DbDataReader> result,
		CancellationToken cancellationToken = default)
	{
		return await processor.ProcessExecutingCommands(command, eventData.Context, result, cancellationToken);
	}

	/// <inheritdoc />
	public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
	{
		return processor.ProcessExecutedCommands(command, eventData.Context, result).GetAwaiter().GetResult();
	}

	/// <inheritdoc />
	public override async ValueTask<object?> ScalarExecutedAsync(DbCommand command,
		CommandExecutedEventData eventData,
		object? result,
		CancellationToken cancellationToken = default)
	{
		return await processor.ProcessExecutedCommands(command, eventData.Context, result, cancellationToken);
	}

	/// <inheritdoc />
	public override InterceptionResult<object> ScalarExecuting(DbCommand command,
		CommandEventData eventData,
		InterceptionResult<object> result)
	{
		return processor.ProcessExecutingCommands(command, eventData.Context, result).GetAwaiter().GetResult();
	}

	/// <inheritdoc />
	public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
		CommandEventData eventData,
		InterceptionResult<object> result,
		CancellationToken cancellationToken = default)

	{
		return await processor.ProcessExecutingCommands(command, eventData.Context, result, cancellationToken);
	}
}
