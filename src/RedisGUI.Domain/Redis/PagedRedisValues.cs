using System;
using System.Collections.Generic;

namespace RedisGUI.Domain.Redis;

/// <summary>
/// Represents a paginated result of Redis values
/// </summary>
public sealed class PagedRedisValues
{
    /// <summary>
    /// Creates a new instance of PagedRedisValues
    /// </summary>
    /// <param name="values">The list of Redis values</param>
    /// <param name="totalCount">Total number of items</param>
    /// <param name="pageNumber">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    private PagedRedisValues(
        IReadOnlyList<RedisValue> values,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        Values = values;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
    }

    /// <summary>
    /// The list of Redis values for the current page
    /// </summary>
    public IReadOnlyList<RedisValue> Values { get; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Indicates whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; }

    /// <summary>
    /// Indicates whether there is a next page
    /// </summary>
    public bool HasNextPage { get; }

    /// <summary>
    /// Creates a new PagedRedisValues instance
    /// </summary>
    /// <param name="values">The list of Redis values</param>
    /// <param name="totalCount">Total number of items</param>
    /// <param name="pageNumber">Current page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>A new PagedRedisValues instance</returns>
    public static PagedRedisValues Create(
        IReadOnlyList<RedisValue> values,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        return new PagedRedisValues(values, totalCount, pageNumber, pageSize);
    }
}
