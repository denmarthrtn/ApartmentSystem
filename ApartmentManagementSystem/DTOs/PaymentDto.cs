namespace ApartmentManagementSystem.DTOs
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int RentChargeId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidDate { get; set; }
        public string Method { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal ChargeBalance { get; set; }   // What is still owed on the charge after this payment
        public string ChargeStatus { get; set; }
    }
}
