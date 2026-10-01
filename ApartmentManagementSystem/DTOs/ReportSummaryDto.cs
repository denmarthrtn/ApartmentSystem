namespace ApartmentManagementSystem.DTOs
{
    // Landlord dashboard numbers, all calculated as of the current date
    public class ReportSummaryDto
    {
        public DateTime AsOf { get; set; }
        public DateTime Month { get; set; }                 // First day of the month the rent figures cover

        // Occupancy
        public int TotalApartments { get; set; }
        public int OccupiedApartments { get; set; }         // Apartments with an active lease
        public int VacantApartments { get; set; }
        public decimal OccupancyRate { get; set; }          // Percent, e.g. 75.00

        // Rent
        public decimal RentBilledThisMonth { get; set; }    // Charges for this billing month
        public decimal RentCollectedThisMonth { get; set; } // Payments received this month (any charge)
        public int OverdueCharges { get; set; }
        public decimal TotalOverdue { get; set; }           // Unpaid balance on overdue charges

        // Maintenance
        public int OpenMaintenanceRequests { get; set; }
        public int InProgressMaintenanceRequests { get; set; }
        public int UrgentMaintenanceRequests { get; set; }  // Urgent and not yet resolved or cancelled
    }
}
