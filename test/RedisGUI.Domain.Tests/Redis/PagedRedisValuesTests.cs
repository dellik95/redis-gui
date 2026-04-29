using RedisGUI.Domain.Redis;
using System.Collections.Generic;

namespace RedisGUI.Domain.Tests.Redis;

/// <summary>
/// Tests for PagedRedisValues domain entity
/// </summary>
[TestFixture]
public class PagedRedisValuesTests
{
	[Test]
	public void Create_ValidValues_ShouldCreateInstance()
	{
		// Arrange
		List<RedisValue> values =
		[
			RedisValue.Create("key1", "value1", "String", null).Value!,
			RedisValue.Create("key2", "value2", "String", null).Value!
		];

		// Act
		PagedRedisValues pagedResult = PagedRedisValues.Create(values, 2, 1, 10);

		// Assert
		pagedResult.Values.Should().HaveCount(2);
		pagedResult.TotalCount.Should().Be(2);
		pagedResult.PageNumber.Should().Be(1);
		pagedResult.PageSize.Should().Be(10);
		pagedResult.TotalPages.Should().Be(1);
		pagedResult.HasPreviousPage.Should().BeFalse();
		pagedResult.HasNextPage.Should().BeFalse();
	}

	[Test]
	public void Create_WithMultiplePages_ShouldCalculateCorrectly()
	{
		// Arrange
		List<RedisValue> values =
		[
			RedisValue.Create("key1", "value1", "String", null).Value!,
			RedisValue.Create("key2", "value2", "String", null).Value!,
			RedisValue.Create("key3", "value3", "String", null).Value!
		];

		// Act
		PagedRedisValues pagedResult = PagedRedisValues.Create(values, 25, 2, 10);

		// Assert
		pagedResult.Values.Should().HaveCount(3);
		pagedResult.TotalCount.Should().Be(25);
		pagedResult.PageNumber.Should().Be(2);
		pagedResult.PageSize.Should().Be(10);
		pagedResult.TotalPages.Should().Be(3);
		pagedResult.HasPreviousPage.Should().BeTrue();
		pagedResult.HasNextPage.Should().BeTrue();
	}

	[Test]
	public void Create_LastPage_ShouldIndicateNoNextPage()
	{
		// Arrange
		List<RedisValue> values =
		[
			RedisValue.Create("key1", "value1", "String", null).Value!
		];

		// Act
		PagedRedisValues pagedResult = PagedRedisValues.Create(values, 21, 3, 10);

		// Assert
		pagedResult.HasPreviousPage.Should().BeTrue();
		pagedResult.HasNextPage.Should().BeFalse();
	}

	[Test]
	public void Create_FirstPage_ShouldIndicateNoPreviousPage()
	{
		// Arrange
		List<RedisValue> values =
		[
			RedisValue.Create("key1", "value1", "String", null).Value!
		];

		// Act
		PagedRedisValues pagedResult = PagedRedisValues.Create(values, 10, 1, 10);

		// Assert
		pagedResult.HasPreviousPage.Should().BeFalse();
		pagedResult.HasNextPage.Should().BeFalse();
	}

	[Test]
	public void Create_ZeroPageSize_ShouldDefaultToOnePage()
	{
		// Arrange
		List<RedisValue> values = [];

		// Act
		PagedRedisValues pagedResult = PagedRedisValues.Create(values, 0, 1, 0);

		// Assert
		pagedResult.TotalPages.Should().Be(1);
	}
}

