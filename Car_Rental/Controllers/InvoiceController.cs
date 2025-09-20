using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Car_Rental.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context) { _context = context; }

        // GET: /Invoice/Pay/{reference}
        // This action shows the payment page. It is already correct.
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

        // POST: /Invoice/PayConfirmation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayConfirmation(string bookingReference)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                .FirstOrDefaultAsync(i => i.Booking.BookingReference == bookingReference);

            if (invoice == null) return NotFound();
            if (invoice.IsPaid) return RedirectToAction("Confirmation", "Booking", new { reference = bookingReference });

            // --- Simulate Payment Processing ---
            invoice.IsPaid = true;
            invoice.PaymentMethod = "Credit Card (Simulated)";
            invoice.Booking.Status = BookingStatus.Booked;

            // ==========================================================
            //                       THE FINAL FIX
            // This logic now correctly handles both Users and Guests.
            // ==========================================================
            var payment = new Payment
            {
                BookingId = invoice.BookingId,
                Amount = invoice.TotalAmount,
                Type = PaymentType.RentalFee,
                Method = PaymentMethod.CreditCard,
                Status = PaymentStatus.Paid,
                PaymentGateway = PaymentGatewayType.Stripe, // Example
                PaymentDate = DateTime.UtcNow,
                TransactionId = $"SIM_{Guid.NewGuid()}"
            };

            // Check if the booking was made by a registered User.
            if (invoice.Booking.UserID.HasValue)
            {
                payment.UserId = invoice.Booking.UserID.Value;
            }
            // If not a User, it must be a Guest. We don't need to link the payment
            // back to the guest table as the booking link is sufficient.
            // If you wanted to, you could add a GuestId to the Payment model.
            // For now, only linking to Users is the standard approach.

            _context.Payments.Add(payment);

            // This logic is now correct as we have removed the driver concept.
            // We can safely delete this block if drivers are no longer part of bookings.
            /*
            if (invoice.Booking.DriverID.HasValue)
            {
                var driver = await _context.Drivers.FindAsync(invoice.Booking.DriverID);
                if (driver != null) driver.Status = DriverStatus.Unavailable;
            }
            */

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment successful! Your booking is confirmed.";
            return RedirectToAction("Confirmation", "Booking", new { reference = bookingReference });
        }
    }
}