using System.Linq.Expressions;
using Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BookManagementSystem.Domain.Pagination;

internal static class OffsetPagination
{
    private const int DefaultPageSize = 10;
    private const int MaximumPageSize = 100;

    public static async Task<OffsetPagedResult<TResult>> CreateAsync<TEntity, TResult>(
        IQueryable<TEntity> filteredQuery,
        OffsetPagedRequest request,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize <= 0
            ? DefaultPageSize
            : Math.Min(request.PageSize, MaximumPageSize);
        var offset = Math.Max(request.Offset, 0);
        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new OffsetPagedResult<TResult>
            {
                TotalCount = 0,
                Offset = 0,
                PageSize = pageSize
            };
        }

        if (offset >= totalCount)
            offset = ((totalCount - 1) / pageSize) * pageSize;

        var items = await orderBy(filteredQuery)
            .Skip(offset)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new OffsetPagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            Offset = offset,
            PageSize = pageSize
        };
    }
}
