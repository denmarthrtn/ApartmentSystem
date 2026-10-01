namespace ApartmentManagementSystem.DTOs
{
    public class RentChargeDto
    {
        public int Id { get; set; }
        public int LeaseId { get; set; }
        public string TenantName { get; set; }
        public string ApartmentName { get; set; }
        public DateTime BillingMonth { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
    }
}
