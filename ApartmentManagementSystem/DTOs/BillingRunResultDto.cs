namespace ApartmentManagementSystem.DTOs
{
    public class BillingRunResultDto
    {
        public DateTime AsOf { get; set; }
        public int ChargesCreated { get; set; }
        public int ChargesMarkedOverdue { get; set; }
    }
}
