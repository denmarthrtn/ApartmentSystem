namespace ApartmentManagementSystem.Models
{
    public enum RentChargeStatus
    {
        Pending,
        Paid,
        Overdue
    }

    // One monthly rent bill for a lease
    public class RentCharge
    {
        public int Id { get; set; }                   // Primary key
        public int LeaseId { get; set; }              // Foreign key to Lease
        public Lease Lease { get; set; }              // Navigation property
        public DateTime BillingMonth { get; set; }    // First day of the month being billed
        public DateTime DueDate { get; set; }         // Payment deadline
        public decimal Amount { get; set; }           // Amount billed (lease rent at time of billing)
        public decimal AmountPaid { get; set; }       // Running total of payments
        public RentChargeStatus Status { get; set; } = RentChargeStatus.Pending;
        public List<Payment> Payments { get; set; } = new();

        public decimal Balance => Amount - AmountPaid;
    }
}
