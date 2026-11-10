using FluentAssertions;
using NUnit.Framework;
using RedisGUI.Domain.Redis;
using System.Collections.Generic;

namespace RedisGUI.Application.Tests.Domain;

[TestFixture]
public class PagedRedisValuesTests
{
    [Test]
    public void Create_FirstPage_CalculatesCorrectPagination()
    {
        // Arrange
        var items = new List<RedisValue>
        {
            RedisValue.Create("key1", "value1", "String").Value,
            RedisValue.Create("key2", "value2", "String").Value,
            RedisValue.Create("key3", "value3", "String").Value
        };

        // Act
        var result = PagedRedisValues.Create(items, 10, 1, 3);

        // Assert
        result.Values.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalPages.Should().Be(4); // 10 items / 3 per page = 4 pages
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Test]
    public void Create_LastPage_CalculatesCorrectPagination()
    {
        // Arrange
        var items = new List<RedisValue>
        {
            RedisValue.Create("key10", "value10", "String").Value
        };

        // Act
        var result = PagedRedisValues.Create(items, 10, 4, 3);

        // Assert
        result.Values.Should().HaveCount(1);
        result.PageNumber.Should().Be(4);
        result.TotalPages.Should().Be(4);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Test]
    public void Create_EmptyPage_HandlesCorrectly()
    {
        // Arrange
        var items = new List<RedisValue>();

        // Act
        var result = PagedRedisValues.Create(items, 0, 1, 10);

        // Assert
        result.Values.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }
}
