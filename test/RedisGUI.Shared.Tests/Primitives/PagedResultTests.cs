using FluentAssertions;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;

namespace RedisGUI.Shared.Tests.Primitives;

/// <summary>
/// Tests for PagedResult record
/// </summary>
[TestFixture]
public class PagedResultTests
{
	[Test]
	public void Create_ValidData_ShouldCreateInstance()
	{
		// Arrange
		List<int> items = [1, 2, 3];

		// Act
		PagedResult<int> pagedResult = new(items, 3, 1, 10);

		// Assert
		pagedResult.Items.Should().HaveCount(3);
		pagedResult.TotalCount.Should().Be(3);
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
		List<int> items = [11, 12, 13];

		// Act
		PagedResult<int> pagedResult = new(items, 25, 2, 10);

		// Assert
		pagedResult.Items.Should().HaveCount(3);
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
		List<int> items = [21];

		// Act
		PagedResult<int> pagedResult = new(items, 21, 3, 10);

		// Assert
		pagedResult.HasPreviousPage.Should().BeTrue();
		pagedResult.HasNextPage.Should().BeFalse();
	}

	[Test]
	public void Create_FirstPage_ShouldIndicateNoPreviousPage()
	{
		// Arrange
		List<int> items = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

		// Act
		PagedResult<int> pagedResult = new(items, 10, 1, 10);

		// Assert
		pagedResult.HasPreviousPage.Should().BeFalse();
		pagedResult.HasNextPage.Should().BeFalse();
	}
}

