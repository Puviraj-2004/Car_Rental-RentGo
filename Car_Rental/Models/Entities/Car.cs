using Car_Rental.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rental.Models.Entities
{
    public class Car
    {
        [Key]
        public int CarId { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string RegistrationNumber { get; set; } = null!;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Brand { get; set; } = null!;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Model { get; set; } = null!;

        [Range(1950, 2025)]
        public int Year { get; set; }

        [Required]
        public FuelType FuelType { get; set; }

        [Required]
        public TransmissionType Transmission { get; set; }

        [Required]
        public CarStatus Status { get; set; } = CarStatus.Available;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RentalPricePerDay { get; set; }

        [StringLength(500)]
        [Url]
        public string? ImageUrl { get; set; }

        // 🔹 Optional Discount Percentage (like 10%)
        [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100.")]
        public decimal? OfferPercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Amount must be positive.")]
        public decimal? OfferAmount { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        // ✅ New Fields
        [Range(1, 100, ErrorMessage = "Seats must be between 1 and 100.")]
        public int NumberOfSeats { get; set; }

        [Required]
        public bool IsAirConditioned { get; set; }   // true = AC, false = Non-AC

        [Range(0, 100, ErrorMessage = "Mileage must be a positive number.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Mileage { get; set; }   // km per liter

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (OfferPercentage.HasValue && OfferAmount.HasValue)
            {
                yield return new ValidationResult(
                    "You can only enter either Offer Percentage or Offer Amount, not both.",
                    new[] { nameof(OfferPercentage), nameof(OfferAmount) });
            }
        }
    }
}
