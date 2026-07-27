namespace Contracts.Pagination;

public class OffsetPagedRequest
{
    public int Offset { get; set; } = 0;

    public int PageSize { get; set; } = 10;
}

public sealed class OffsetPagedResult<T>
{
    public List<T> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Offset { get; set; }

    public int PageSize { get; set; } = 10;
}
