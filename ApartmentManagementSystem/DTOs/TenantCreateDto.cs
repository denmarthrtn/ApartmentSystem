namespace ApartmentManagementSystem.DTOs
{

    public class TenantCreateDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PermanentAddress { get; set; }
        public DateTime MoveInDate { get; set; }
        public int ApartmentId { get; set; }
    }
}
