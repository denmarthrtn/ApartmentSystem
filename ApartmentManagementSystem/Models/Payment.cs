namespace ApartmentManagementSystem.Models
{
    public enum PaymentMethod
    {
        Cash,
        BankTransfer,
        EWallet
    }

    // Money received against a rent charge (a charge can have several partial payments)
    public class Payment
    {
        public int Id { get; set; }                    // Primary key
        public int RentChargeId { get; set; }          // Foreign key to RentCharge
        public RentCharge RentCharge { get; set; }     // Navigation property
        public decimal Amount { get; set; }            // Amount received
        public DateTime PaidDate { get; set; }         // When it was received
        public PaymentMethod Method { get; set; }      // How it was paid
        public string? ReferenceNumber { get; set; }   // Bank / e-wallet reference, optional
    }
}
