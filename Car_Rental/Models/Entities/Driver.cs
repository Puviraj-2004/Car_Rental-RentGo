using Car_Rental.Enum;
using System.ComponentModel.DataAnnotations;

namespace Car_Rental.Models.Entities
{
    public class Driver
    {
        [Key]
        public int DriverID { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string FullName { get; set; } = null!;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = null!;

        [Required]
        [StringLength(12, MinimumLength = 10)]
        public string NIC { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string DriverLicenseNumber { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        public DateTime LicenseExpiryDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DriverStatus Status { get; set; } = DriverStatus.Available;

        [StringLength(500)]
        [Url]
        public string? PhotoUrl { get; set; }

        [Range(0, 50000, ErrorMessage = "Fee per day must be between 0 and 50,000.")]
        public decimal? FeePerDay { get; set; }

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
