namespace ApartmentManagementSystem.DTOs
{
    public class LeaseBalanceDto
    {
        public int LeaseId { get; set; }
        public decimal TotalCharged { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public decimal OverdueAmount { get; set; }
        public int OverdueCount { get; set; }
    }
}
