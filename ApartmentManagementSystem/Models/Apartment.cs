namespace ApartmentManagementSystem.Models
{
    public class Apartment
    {
        public int Id { get; set; }          // Primary key
        public string Name { get; set; }     // Apartment name
        public string Location { get; set; } // City or area
        public string Address { get; set; } // Street Level detail
        public decimal Rent { get; set; }    // Monthly rent
        public int Rooms { get; set; }       // Number of rooms

        //New Colum
        public bool HasParking { get; set; }
    }
}
