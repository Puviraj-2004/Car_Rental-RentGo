using Car_Rental.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rental.Models.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // Foreign Keys
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? GuestId { get; set; }
        public Guest? Guest { get; set; }

        // Payment details
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000.00, ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment type is required.")]
        public PaymentType Type { get; set; }  // Deposit, Rental, Damage, Insurance, etc.

        [Required(ErrorMessage = "Payment method is required.")]
        public PaymentMethod Method { get; set; }

        [Required(ErrorMessage = "Payment status is required.")]
        public PaymentStatus Status { get; set; }

        [Required(ErrorMessage = "Payment gateway type is required.")]
        public PaymentGatewayType PaymentGateway { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? TransactionId { get; set; }

        // Card Details (for saved cards)
        [StringLength(4)]
        public string? CardLastFourDigits { get; set; }

        [StringLength(50)]
        public string? CardHolderName { get; set; }

        [StringLength(20)]
        public string? CardType { get; set; } // Visa, MasterCard, etc.

        [StringLength(7)]
        public string? CardExpiryMonth { get; set; } // MM/YY format

        public bool SaveCard { get; set; } = false;
    }
}
