using ApartmentManagementSystem.Security;

namespace ApartmentManagementSystem.Tests
{
    // Fixed "today" so tests don't depend on the real date
    public class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTime now) => _now = new DateTimeOffset(now, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    // Stand-in for the logged-in user
    public class TestCurrentUser : ICurrentUser
    {
        public bool IsAdmin { get; init; }
        public int? TenantId { get; init; }

        public static TestCurrentUser Admin => new() { IsAdmin = true };
        public static TestCurrentUser ForTenant(int tenantId) => new() { TenantId = tenantId };
    }
}
