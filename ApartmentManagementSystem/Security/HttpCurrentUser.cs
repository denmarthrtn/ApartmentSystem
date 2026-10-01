namespace ApartmentManagementSystem.Security
{
    // Reads the current user from the JWT claims of the request.
    // Outside a request (e.g. the background billing job) there is no user: not admin, no tenant.
    public class HttpCurrentUser : ICurrentUser
    {
        public const string TenantIdClaim = "tenantId";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole(Roles.Admin) ?? false;

        public int? TenantId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirst(TenantIdClaim)?.Value;
                return int.TryParse(value, out var id) ? id : null;
            }
        }
    }
}
