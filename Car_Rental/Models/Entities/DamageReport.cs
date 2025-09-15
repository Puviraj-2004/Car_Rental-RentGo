using Car_Rental.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rental.Models.Entities
{
    public class DamageReport
    {
        [Key]
        public int DamageReportId { get; set; }

        [Required]
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public int? InsuranceID { get; set; }
        public Insurance? Insurance { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 10)]
        public string Description { get; set; } = null!;

        public DateTime ReportedDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedRepairCost { get; set; }

        public bool IsResolved { get; set; } = false;

        [StringLength(1000)]
        public string? DamageImageUrls { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClaimAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UserPayAmount { get; set; }
    }
}
