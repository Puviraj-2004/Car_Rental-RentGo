using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rental.Models.Entities
{
    public class DiscountCode
    {
        [Key]
        public int DiscountCodeId { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string Code { get; set; } = null!;

        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FixedAmountDiscount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Range(1, int.MaxValue)]
        public int UsageLimit { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();




        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DiscountPercentage.HasValue && FixedAmountDiscount.HasValue)
            {
                yield return new ValidationResult(
                    "You can set either Discount Percentage OR Fixed Amount, not both.",
                    new[] { nameof(DiscountPercentage), nameof(FixedAmountDiscount) });
            }

            if (!DiscountPercentage.HasValue && !FixedAmountDiscount.HasValue)
            {
                yield return new ValidationResult(
                    "You must set either Discount Percentage OR Fixed Amount.",
                    new[] { nameof(DiscountPercentage), nameof(FixedAmountDiscount) });
            }
        }
    }
}
