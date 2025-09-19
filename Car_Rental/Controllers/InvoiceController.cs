using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Car_Rental.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context) { _context = context; }

        // GET: /Invoice/Pay/{reference}
        // This action shows the payment page. It remains named "Pay".
        [HttpGet("Invoice/Pay/{reference}")]
        public async Task<IActionResult> Pay(string reference)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                .FirstOrDefaultAsync(i => i.Booking.BookingReference == reference);

            if (invoice == null) return NotFound();
            if (invoice.IsPaid)
            {
                TempData["InfoMessage"] = "This booking is already paid and confirmed.";
                return RedirectToAction("Confirmation", "Booking", new { reference });
            }
            return View(invoice);
        }

        // ==========================================================
        //                       THE CORRECTION
        // The POST action is renamed from "Pay" to "PayConfirmation"
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayConfirmation(string bookingReference) // The form POSTS here
        {
            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                .FirstOrDefaultAsync(i => i.Booking.BookingReference == bookingReference);

            if (invoice == null) return NotFound();

            // --- Simulate Payment Processing ---
            invoice.IsPaid = true;
            invoice.PaymentMethod = "Credit Card (Simulated)";
            invoice.Booking.Status = BookingStatus.Confirmed;

            // Create a Payment record
            var payment = new Payment
            {
                BookingId = invoice.BookingId,
                //UserId = invoice.Booking.UserID,
                Amount = invoice.TotalAmount,
                Type = PaymentType.RentalFee,
                Method = PaymentMethod.CreditCard,
                Status = PaymentStatus.Paid,
                PaymentGateway = PaymentGatewayType.Stripe,
                PaymentDate = DateTime.UtcNow,
                TransactionId = $"SIM_{Guid.NewGuid()}"
            };
            _context.Payments.Add(payment);

            if (invoice.Booking.DriverID.HasValue)
            {
                var driver = await _context.Drivers.FindAsync(invoice.Booking.DriverID);
                if (driver != null) driver.Status = DriverStatus.Unavailable;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment successful! Your booking is confirmed.";
            return RedirectToAction("Confirmation", "Booking", new { reference = bookingReference });
        }
    }
}