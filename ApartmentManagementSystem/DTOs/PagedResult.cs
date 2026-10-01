namespace ApartmentManagementSystem.DTOs
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;

        // Convert items (e.g. entity -> DTO) while keeping paging info
        public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) => new PagedResult<TOut>
        {
            Items = Items.Select(map).ToList(),
            Page = Page,
            PageSize = PageSize,
            TotalCount = TotalCount
        };
    }
}
