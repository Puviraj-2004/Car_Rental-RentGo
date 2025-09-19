using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rental.Models.Entities
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required(ErrorMessage = "Booking ID is required.")]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        [Required(ErrorMessage = "Invoice date is required.")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Range(0.01, 1000000.00, ErrorMessage = "Subtotal must be greater than 0.")]
        public decimal Subtotal { get; set; }

        [Range(0.00, 1000000.00, ErrorMessage = "Discount cannot be negative.")]
        public decimal? DiscountAmount { get; set; } = 0;

        // 1. புதிதாக சேர்க்கப்பட்ட ExtraFee
        [Display(Name = "Extra Fee")]
        [Range(0.00, 1000000.00, ErrorMessage = "Extra fee cannot be negative.")]
        public decimal? ExtraFee { get; set; } = 0;

        // 2. புதிதாக சேர்க்கப்பட்ட Reason
        [Display(Name = "Reason for Extra Fee")]
        [StringLength(200, ErrorMessage = "The reason cannot exceed 200 characters.")]
        public string ExtraFeeReason { get; set; }

        [Range(0.01, 1000000.00, ErrorMessage = "Total amount must be greater than 0.")]
        public decimal TotalAmount { get; set; }

        public bool IsPaid { get; set; } = false;

        [StringLength(50)]
        public string PaymentMethod { get; set; }
    }
}