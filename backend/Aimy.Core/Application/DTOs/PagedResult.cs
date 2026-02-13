namespace Aimy.Core.Application.DTOs;

public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; set; }
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public required int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
