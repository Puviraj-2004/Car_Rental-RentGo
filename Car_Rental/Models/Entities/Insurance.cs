using System.ComponentModel.DataAnnotations;

namespace Car_Rental.Models.Entities
{
    public class Insurance
    {
        [Key]
        public int InsuranceID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [Range(1, 100)]
        public decimal CoveragePercentage { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        //public ICollection<DamageReport> DamageReports { get; set; } = new List<DamageReport>();
    }
}
