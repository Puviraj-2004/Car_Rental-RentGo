using Car_Rental.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rental.Models.Entities
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [Required]
        [StringLength(20)]
        public string BookingReference { get; set; } = null!;

        [Required]
        public int UserID { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int CarID { get; set; }
        public Car Car { get; set; } = null!;

        [Required]
        public int? DriverID { get; set; }
        public Driver? Driver { get; set; } = null!;

        public int? InsuranceID { get; set; }
        public Insurance? Insurance { get; set; }

        [Required(ErrorMessage = "Booking date is required.")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PickupDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        [StringLength(20)]
        public string DriverLicenseNumber { get; set; } = null!;

        [Required]
        public DateTime LicenseExpiryDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(12, MinimumLength = 10)]
        public string NIC { get; set; } = null!;

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.pending;

        // Navigation Properties
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}