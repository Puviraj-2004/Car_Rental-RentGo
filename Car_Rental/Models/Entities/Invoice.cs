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

        // This must be singular and match DbContext mapping
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; }

        [Required(ErrorMessage = "Invoice date is required.")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Range(0.01, 1000000.00, ErrorMessage = "Subtotal must be greater than 0.")]
        public decimal Subtotal { get; set; }

        [Range(0.00, 1000000.00, ErrorMessage = "Discount cannot be negative.")]
        public decimal DiscountAmount { get; set; } = 0;

        [Range(0.01, 1000000.00, ErrorMessage = "Total amount must be greater than 0.")]
        public decimal TotalAmount { get; set; }

        public bool IsPaid { get; set; } = false;

        [StringLength(50)]
        public string PaymentMethod { get; set; }
    }
}
