using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.DTOs
{
    public class PaymentCreateDto
    {
        public decimal Amount { get; set; }
        public DateTime PaidDate { get; set; }
        public PaymentMethod Method { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
