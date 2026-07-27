using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace BookManagementSystem.Domain.Pagination;

internal static class OffsetPagination
{
    public static async Task<OffsetPagedResult<TResult>> CreateAsync<TEntity, TResult>(
        IQueryable<TEntity> filteredQuery,
        OffsetPagedRequest request,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken)
    {
        var pageSize = request.PageSize;
        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new OffsetPagedResult<TResult>
            {
                TotalCount = 0,
                Page = 1,
                PageSize = pageSize
            };
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Min(request.Page, totalPages);
        var offset = (page - 1) * pageSize;

        var items = await orderBy(filteredQuery)
            .Skip(offset)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new OffsetPagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
