namespace Aimy.Core.Application.DTOs;

/// <summary>
/// Generic paginated result wrapper
/// </summary>
/// <typeparam name="T">Type of items in the result set</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Collection of items for the current page
    /// </summary>
    public required IReadOnlyList<T> Items { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    /// <example>1</example>
    public required int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    /// <example>10</example>
    public required int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    /// <example>42</example>
    public required int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages (calculated)
    /// </summary>
    /// <example>5</example>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
