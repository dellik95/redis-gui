using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Primitives;

/// <summary>
/// Extension methods for working with async enumerables and large datasets
/// </summary>
public static class AsyncEnumerableExtensions
{
	/// <summary>
	/// Converts an async enumerable to a list asynchronously
	/// </summary>
	/// <typeparam name="T">Item type</typeparam>
	/// <param name="source">The async enumerable source</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A list containing all items</returns>
	public static async Task<List<T>> ToListAsync<T>(
		this IAsyncEnumerable<T> source,
		CancellationToken cancellationToken = default)
	{
		List<T> list = [];
		await foreach (T? item in source.WithCancellation(cancellationToken))
		{
			list.Add(item);
		}
		return list;
	}

	/// <summary>
	/// Converts an async enumerable to an array asynchronously
	/// </summary>
	/// <typeparam name="T">Item type</typeparam>
	/// <param name="source">The async enumerable source</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>An array containing all items</returns>
	public static async Task<T[]> ToArrayAsync<T>(
		this IAsyncEnumerable<T> source,
		CancellationToken cancellationToken = default)
	{
		List<T> list = await source.ToListAsync(cancellationToken);
		return list.ToArray();
	}

	/// <summary>
	/// Batches items from an async enumerable into chunks of specified size
	/// </summary>
	/// <typeparam name="T">Item type</typeparam>
	/// <param name="source">The async enumerable source</param>
	/// <param name="batchSize">Size of each batch</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Async enumerable of batches</returns>
	public static async IAsyncEnumerable<List<T>> BatchAsync<T>(
		this IAsyncEnumerable<T> source,
		int batchSize,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		List<T> batch = new(batchSize);
		await foreach (T? item in source.WithCancellation(cancellationToken))
		{
			batch.Add(item);
			if (batch.Count >= batchSize)
			{
				yield return batch;
				batch = new List<T>(batchSize);
			}
		}

		if (batch.Count > 0)
		{
			yield return batch;
		}
	}
}

