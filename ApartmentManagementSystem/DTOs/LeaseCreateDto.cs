namespace ApartmentManagementSystem.DTOs
{
    public class LeaseCreateDto
    {
        public int TenantId { get; set; }
        public int ApartmentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal SecurityDeposit { get; set; }
    }
}
