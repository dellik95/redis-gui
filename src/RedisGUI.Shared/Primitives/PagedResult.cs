namespace RedisGUI.Shared.Primitives;

/// <summary>
/// Represents a paginated result set
/// </summary>
/// <param name="Items">The items in the current page</param>
/// <param name="TotalCount">Total number of items across all pages</param>
/// <param name="PageNumber">Current page number (1-based)</param>
/// <param name="PageSize">Number of items per page</param>
public sealed record PagedResult<T>(
	IReadOnlyList<T> Items,
	int TotalCount,
	int PageNumber,
	int PageSize)
{
	/// <summary>
	/// Gets the total number of pages
	/// </summary>
	public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

	/// <summary>
	/// Gets whether there is a next page
	/// </summary>
	public bool HasNextPage => PageNumber < TotalPages;

	/// <summary>
	/// Gets whether there is a previous page
	/// </summary>
	public bool HasPreviousPage => PageNumber > 1;

	/// <summary>
	/// Gets whether the result set is empty
	/// </summary>
	public bool IsEmpty => Items.Count == 0;
}

