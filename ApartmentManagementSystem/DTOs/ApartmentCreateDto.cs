namespace ApartmentManagementSystem.DTOs
{
    public class ApartmentCreateDto
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Address { get; set; }
        public decimal Rent { get; set; }
        public int Rooms { get; set; }
        public bool HasParking { get; set; }
    }
}
