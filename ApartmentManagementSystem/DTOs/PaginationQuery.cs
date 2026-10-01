namespace ApartmentManagementSystem.DTOs
{
    // Bound from the query string: ?page=1&pageSize=10
    public class PaginationQuery
    {
        public const int MaxPageSize = 50;

        private int _page = 1;
        private int _pageSize = 10;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        // Clamped so a client can't request ?pageSize=1000000 and load the whole table
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 1 : Math.Min(value, MaxPageSize);
        }
    }
}
