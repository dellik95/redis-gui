using FluentAssertions;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Tests.Primitives;

/// <summary>
/// Tests for AsyncEnumerableExtensions
/// </summary>
[TestFixture]
public class AsyncEnumerableExtensionsTests
{
	[Test]
	public async Task ToListAsync_WithItems_ReturnsList()
	{
		// Arrange
		int[] items = new[] { 1, 2, 3, 4, 5 };
		IAsyncEnumerable<int> asyncEnumerable = GetAsyncEnumerable(items);

		// Act
		List<int> result = await AsyncEnumerableExtensions.ToListAsync(asyncEnumerable);

		// Assert
		result.Should().NotBeNull();
		result.Should().HaveCount(5);
		result.Should().BeEquivalentTo(items);
	}

	[Test]
	public async Task ToListAsync_EmptyEnumerable_ReturnsEmptyList()
	{
		// Arrange
		IAsyncEnumerable<int> asyncEnumerable = GetAsyncEnumerable(System.Array.Empty<int>());

		// Act
		List<int> result = await AsyncEnumerableExtensions.ToListAsync(asyncEnumerable);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
	}

	[Test]
	public async Task ToArrayAsync_WithItems_ReturnsArray()
	{
		// Arrange
		int[] items = new[] { 10, 20, 30 };
		IAsyncEnumerable<int> asyncEnumerable = GetAsyncEnumerable(items);

		// Act
		int[] result = await AsyncEnumerableExtensions.ToArrayAsync(asyncEnumerable);

		// Assert
		result.Should().NotBeNull();
		result.Should().HaveCount(3);
		result.Should().BeEquivalentTo(items);
		result.Should().BeOfType<int[]>();
	}

	[Test]
	public async Task ToArrayAsync_EmptyEnumerable_ReturnsEmptyArray()
	{
		// Arrange
		IAsyncEnumerable<string> asyncEnumerable = GetAsyncEnumerable(System.Array.Empty<string>());

		// Act
		string[] result = await AsyncEnumerableExtensions.ToArrayAsync(asyncEnumerable);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeEmpty();
		result.Should().BeOfType<string[]>();
	}

	[Test]
	public async Task BatchAsync_WithItems_ReturnsBatches()
	{
		// Arrange
		int[] items = new[] { 1, 2, 3, 4, 5, 6, 7 };
		IAsyncEnumerable<int> asyncEnumerable = GetAsyncEnumerable(items);
		int batchSize = 3;

		// Act
		List<List<int>> batches = new();
		await foreach (List<int> batch in AsyncEnumerableExtensions.BatchAsync(asyncEnumerable, batchSize))
		{
			batches.Add(batch);
		}

		// Assert
		batches.Should().HaveCount(3);
		batches[0].Should().BeEquivalentTo([1, 2, 3]);
		batches[1].Should().BeEquivalentTo([4, 5, 6]);
		batches[2].Should().BeEquivalentTo([7]);
	}

	[Test]
	public async Task BatchAsync_ExactBatchSize_ReturnsSingleBatch()
	{
		// Arrange
		int[] items = new[] { 1, 2, 3 };
		IAsyncEnumerable<int> asyncEnumerable = GetAsyncEnumerable(items);
		int batchSize = 3;

		// Act
		List<List<int>> batches = new();
		await foreach (List<int> batch in AsyncEnumerableExtensions.BatchAsync(asyncEnumerable, batchSize))
		{
			batches.Add(batch);
		}

		// Assert
		batches.Should().HaveCount(1);
		batches[0].Should().BeEquivalentTo(items);
	}

	[Test]
	public async Task BatchAsync_EmptyEnumerable_ReturnsNoBatches()
	{
		// Arrange
		IAsyncEnumerable<int> asyncEnumerable = GetAsyncEnumerable(System.Array.Empty<int>());
		int batchSize = 5;

		// Act
		List<List<int>> batches = new();
		await foreach (List<int> batch in AsyncEnumerableExtensions.BatchAsync(asyncEnumerable, batchSize))
		{
			batches.Add(batch);
		}

		// Assert
		batches.Should().BeEmpty();
	}

	[Test]
	public async Task BatchAsync_SingleItem_ReturnsSingleItemBatch()
	{
		// Arrange
		int[] items = new[] { 42 };
		IAsyncEnumerable<int> asyncEnumerable = GetAsyncEnumerable(items);
		int batchSize = 5;

		// Act
		List<List<int>> batches = new();
		await foreach (List<int> batch in AsyncEnumerableExtensions.BatchAsync(asyncEnumerable, batchSize))
		{
			batches.Add(batch);
		}

		// Assert
		batches.Should().HaveCount(1);
		batches[0].Should().BeEquivalentTo(items);
	}

	private static async IAsyncEnumerable<T> GetAsyncEnumerable<T>(IEnumerable<T> items)
	{
		foreach (T? item in items)
		{
			await Task.Yield();
			yield return item;
		}
	}
}

