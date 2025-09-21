using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show payment page
        [HttpGet]
        public async Task<IActionResult> Pay(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Insurance)
                .Include(b => b.User)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("Home", "Guest");
            }

            // Calculate pricing details
            var totalDays = (booking.ReturnDate - booking.PickupDate).Days;
            var carCost = booking.Car.RentalPricePerDay * totalDays;
            var insuranceCost = booking.Insurance != null ? 
                (carCost * booking.Insurance.CoveragePercentage / 100) : 0;

            ViewBag.TotalDays = totalDays;
            ViewBag.CarCost = carCost;
            ViewBag.InsuranceCost = insuranceCost;
            ViewBag.TotalPrice = booking.TotalPrice;

            return View(booking);
        }

        // Process payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int bookingId, string paymentMethod, string? cardNumber, string? expiryDate, string? cvv, string? cardHolderName)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Car)
                    .Include(b => b.Insurance)
                    .Include(b => b.User)
                    .Include(b => b.Guest)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("Home", "Guest");
                }

                // Validate payment method
                if (string.IsNullOrEmpty(paymentMethod))
                {
                    TempData["ErrorMessage"] = "Please select a payment method.";
                    return RedirectToAction("Pay", new { bookingId });
                }

                // For card payments, validate card details
                if (paymentMethod == "CreditCard" || paymentMethod == "DebitCard")
                {
                    if (string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(expiryDate) || 
                        string.IsNullOrEmpty(cvv) || string.IsNullOrEmpty(cardHolderName))
                    {
                        TempData["ErrorMessage"] = "Please fill in all card details.";
                        return RedirectToAction("Pay", new { bookingId });
                    }
                }

                // Create payment record
                var payment = new Payment
                {
                    BookingId = bookingId,
                    UserId = booking.UserID ?? 1, // Default for guest users
                    Amount = booking.TotalPrice,
                    Type = PaymentType.RentalFee,
                    Method = (PaymentMethod)System.Enum.Parse(typeof(PaymentMethod), paymentMethod),
                    Status = PaymentStatus.Paid,
                    PaymentGateway = PaymentGatewayType.Stripe,
                    PaymentDate = DateTime.UtcNow,
                    TransactionId = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{bookingId}"
                };

                _context.Payments.Add(payment);

                // Update booking status
                booking.Status = BookingStatus.Booked;
                await _context.SaveChangesAsync();

                // Store success message and booking reference
                TempData["SuccessMessage"] = "Payment completed successfully!";
                TempData["BookingReference"] = booking.BookingReference;
                TempData["TransactionId"] = payment.TransactionId;

                return RedirectToAction("Success", new { bookingId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Payment failed: {ex.Message}";
                return RedirectToAction("Pay", new { bookingId });
            }
        }

        // Payment success page
        [HttpGet]
        public async Task<IActionResult> Success(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Insurance)
                .Include(b => b.User)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("Home", "Guest");
            }

            return View(booking);
        }
    }
}