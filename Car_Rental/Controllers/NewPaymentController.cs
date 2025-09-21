using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Controllers
{
    public class NewPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewPaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show payment page
        [HttpGet]
        public async Task<IActionResult> Pay(int bookingId)
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

                // Calculate pricing
                var totalDays = (booking.ReturnDate - booking.PickupDate).Days;
                if (totalDays <= 0) totalDays = 1;

                var carCost = totalDays * booking.Car.RentalPricePerDay;
                decimal insuranceCost = 0;

                if (booking.Insurance != null)
                {
                    insuranceCost = carCost * (booking.Insurance.CoveragePercentage / 100);
                }

                var totalPrice = carCost + insuranceCost;

                // Update booking total price
                booking.TotalPrice = totalPrice;
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                // Pass data to view
                ViewBag.TotalDays = totalDays;
                ViewBag.CarCost = carCost;
                ViewBag.InsuranceCost = insuranceCost;
                ViewBag.TotalPrice = totalPrice;

                return View(booking);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading payment page: " + ex.Message;
                return RedirectToAction("Home", "Guest");
            }
        }

        // Process payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int bookingId, string paymentMethod = "CreditCard")
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Car)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("Home", "Guest");
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
                _context.Bookings.Update(booking);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment completed successfully!";
                return RedirectToAction("Success", new { bookingId = bookingId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Payment failed: " + ex.Message;
                return RedirectToAction("Pay", new { bookingId = bookingId });
            }
        }

        // Payment success page
        [HttpGet]
        public async Task<IActionResult> Success(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Car)
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading success page: " + ex.Message;
                return RedirectToAction("Home", "Guest");
            }
        }
    }
}
