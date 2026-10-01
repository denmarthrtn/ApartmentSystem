using ApartmentManagementSystem.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Extensions
{
    public static class QueryableExtensions
    {
        // Runs COUNT + a single page query in SQL (OFFSET/FETCH), so only one page is loaded into memory.
        // The query must be ordered first, otherwise page contents are not stable.
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IOrderedQueryable<T> query, PaginationQuery pagination)
        {
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
