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

            // Get saved cards for user or guest
            var savedCards = new List<Payment>();
            if (booking.UserID.HasValue)
            {
                savedCards = await _context.Payments
                    .Where(p => p.UserId == booking.UserID &&
                               !string.IsNullOrEmpty(p.CardLastFourDigits) &&
                               p.SaveCard == true)
                    .GroupBy(p => new { p.CardLastFourDigits, p.CardType, p.CardHolderName })
                    .Select(g => g.OrderByDescending(p => p.PaymentDate).First())
                    .ToListAsync();
            }
            else if (booking.GuestID.HasValue)
            {
                savedCards = await _context.Payments
                    .Where(p => p.GuestId == booking.GuestID &&
                               !string.IsNullOrEmpty(p.CardLastFourDigits) &&
                               p.SaveCard == true)
                    .GroupBy(p => new { p.CardLastFourDigits, p.CardType, p.CardHolderName })
                    .Select(g => g.OrderByDescending(p => p.PaymentDate).First())
                    .ToListAsync();
            }

            ViewBag.SavedCards = savedCards;
            return View(booking);
        }

        // Process payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int bookingId, string paymentMethod, string? cardNumber, string? expiryDate, string? cvv, string? cardHolderName, bool saveCard = false, int? savedCardId = null)
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

                // Handle saved card or new card
                Payment? savedCardPayment = null;
                if (savedCardId.HasValue)
                {
                    savedCardPayment = await _context.Payments.FindAsync(savedCardId.Value);
                }

                // For card payments, validate card details (unless using saved card)
                if (paymentMethod == "CreditCard" || paymentMethod == "DebitCard")
                {
                    if (savedCardPayment == null)
                    {
                        if (string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(expiryDate) ||
                            string.IsNullOrEmpty(cvv) || string.IsNullOrEmpty(cardHolderName))
                        {
                            TempData["ErrorMessage"] = "Please fill in all card details.";
                            return RedirectToAction("Pay", new { bookingId });
                        }
                    }
                }

                // Create payment record
                var payment = new Payment
                {
                    BookingId = bookingId,
                    UserId = booking.UserID,
                    GuestId = booking.GuestID,
                    Amount = booking.TotalPrice,
                    Type = PaymentType.RentalFee,
                    Method = (PaymentMethod)System.Enum.Parse(typeof(PaymentMethod), paymentMethod),
                    Status = PaymentStatus.Paid,
                    PaymentGateway = PaymentGatewayType.Stripe,
                    PaymentDate = DateTime.UtcNow,
                    TransactionId = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{bookingId}",
                    SaveCard = saveCard
                };

                // Add card details if saving or using saved card
                if (savedCardPayment != null)
                {
                    payment.CardLastFourDigits = savedCardPayment.CardLastFourDigits;
                    payment.CardHolderName = savedCardPayment.CardHolderName;
                    payment.CardType = savedCardPayment.CardType;
                    payment.CardExpiryMonth = savedCardPayment.CardExpiryMonth;
                }
                else if ((paymentMethod == "CreditCard" || paymentMethod == "DebitCard") && !string.IsNullOrEmpty(cardNumber))
                {
                    payment.CardLastFourDigits = cardNumber.Substring(cardNumber.Length - 4);
                    payment.CardHolderName = cardHolderName;
                    payment.CardType = GetCardType(cardNumber);
                    payment.CardExpiryMonth = expiryDate;
                }

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

        // Helper method to detect card type
        private string GetCardType(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return "Unknown";

            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            if (cardNumber.StartsWith("4"))
                return "Visa";
            else if (cardNumber.StartsWith("5") || (cardNumber.Length >= 4 && int.Parse(cardNumber.Substring(0, 4)) >= 2221 && int.Parse(cardNumber.Substring(0, 4)) <= 2720))
                return "MasterCard";
            else if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37"))
                return "American Express";
            else if (cardNumber.StartsWith("6011") || cardNumber.StartsWith("65"))
                return "Discover";
            else
                return "Unknown";
        }
    }
}