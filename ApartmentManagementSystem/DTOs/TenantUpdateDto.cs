namespace ApartmentManagementSystem.DTOs
{
    public class TenantUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PermanentAddress { get; set; }
        public DateTime MoveInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public int ApartmentId { get; set; }
    }
}
