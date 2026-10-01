using ApartmentManagementSystem.Models;

public class Tenant
{
    public int Id { get; set; }              // Primary key
    public string Name { get; set; }         // Full name
    public string Email { get; set; }        // Contact email
    public string PhoneNumber { get; set; }  // Contact number
    public string PermanentAddress { get; set; } //Tenant Permanent address or Mailing Address
    public DateTime MoveInDate { get; set; } // When tenant moved in
    public DateTime? MoveOutDate { get; set; } // Optional, if tenant leaves
    public int ApartmentId { get; set; }     // Foreign key to Apartment
    public Apartment Apartment { get; set; } // Navigation property
}
