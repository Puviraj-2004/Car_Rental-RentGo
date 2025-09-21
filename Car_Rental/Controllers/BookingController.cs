using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Car_Rental.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Show booking form for a specific car
        [HttpGet]
        public async Task<IActionResult> Book(int carId, DateTime? pickupDate, DateTime? returnDate)
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null)
            {
                TempData["ErrorMessage"] = "Car not found.";
                return RedirectToAction("Cars", "Guest");
            }

            var booking = new Booking
            {
                CarID = carId,
                Car = car,
                PickupDate = pickupDate ?? DateTime.Now.AddDays(1),
                ReturnDate = returnDate ?? DateTime.Now.AddDays(3)
            };

            // Get available insurances
            ViewBag.Insurances = await _context.Insurances.ToListAsync();
            
            return View(booking);
        }

        // POST: Create booking and generate reference code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Booking booking, string? guestFullName, string? guestEmail, string? guestPhoneNumber)
        {
            try
            {
                // Generate unique booking reference
                booking.BookingReference = GenerateBookingReference();
                
                // Handle user authentication
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdString, out int userId))
                    {
                        booking.UserID = userId;
                    }
                }
                else
                {
                    // Handle guest booking
                    if (string.IsNullOrEmpty(guestFullName) || string.IsNullOrEmpty(guestEmail) || string.IsNullOrEmpty(guestPhoneNumber))
                    {
                        TempData["ErrorMessage"] = "Please fill in all guest details.";
                        return await ReturnBookingView(booking);
                    }

                    var guest = new Guest
                    {
                        FullName = guestFullName,
                        Email = guestEmail,
                        PhoneNumber = guestPhoneNumber
                    };

                    _context.Guests.Add(guest);
                    await _context.SaveChangesAsync();
                    booking.GuestID = guest.Id;
                }

                // Calculate total price
                var car = await _context.Cars.FindAsync(booking.CarID);
                if (car == null)
                {
                    TempData["ErrorMessage"] = "Car not found.";
                    return await ReturnBookingView(booking);
                }
                
                var insurance = await _context.Insurances.FindAsync(booking.InsuranceID);
                
                var totalDays = (booking.ReturnDate - booking.PickupDate).Days;
                var carCost = car.RentalPricePerDay * totalDays;
                var insuranceCost = insurance != null ? (carCost * insurance.CoveragePercentage / 100) : 0;
                
                booking.TotalPrice = carCost + insuranceCost;
                booking.Status = BookingStatus.pending;
                booking.BookingDate = DateTime.UtcNow;

                // Save booking
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Redirect to payment
                return RedirectToAction("Pay", "Payment", new { bookingId = booking.BookingID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return await ReturnBookingView(booking);
            }
        }

        // My Bookings - Find booking by reference code
        [HttpGet]
        public IActionResult MyBookings()
        {
            return View();
        }

        // Find booking by reference code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindBooking(string bookingReference)
        {
            if (string.IsNullOrEmpty(bookingReference))
            {
                TempData["ErrorMessage"] = "Please enter a booking reference code.";
                return View("MyBookings");
            }

            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Insurance)
                .Include(b => b.User)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.BookingReference == bookingReference.ToUpper());

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found. Please check your reference code.";
                return View("MyBookings");
            }

            return View("BookingDetails", booking);
        }

        // Cancel booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId, string bookingReference)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            
            if (booking == null || booking.BookingReference != bookingReference)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Booking is already cancelled.";
                return RedirectToAction("FindBooking", new { bookingReference });
            }

            // Check if booking can be cancelled (e.g., at least 24 hours before pickup)
            if (booking.PickupDate <= DateTime.Now.AddHours(24))
            {
                TempData["ErrorMessage"] = "Cannot cancel booking less than 24 hours before pickup time.";
                return RedirectToAction("FindBooking", new { bookingReference });
            }

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Booking cancelled successfully.";
            return RedirectToAction("FindBooking", new { bookingReference });
        }

        // Extend rental period
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendBooking(int bookingId, string bookingReference, DateTime newReturnDate)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Insurance)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.BookingReference == bookingReference);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (newReturnDate <= booking.ReturnDate)
            {
                TempData["ErrorMessage"] = "New return date must be after the current return date.";
                return RedirectToAction("FindBooking", new { bookingReference });
            }

            // Check if car is available for extended period
            var conflictingBookings = await _context.Bookings
                .Where(b => b.CarID == booking.CarID && 
                           b.BookingID != booking.BookingID &&
                           b.Status != BookingStatus.Cancelled &&
                           b.PickupDate < newReturnDate &&
                           b.ReturnDate > booking.ReturnDate)
                .AnyAsync();

            if (conflictingBookings)
            {
                TempData["ErrorMessage"] = "Car is not available for the extended period.";
                return RedirectToAction("FindBooking", new { bookingReference });
            }

            // Calculate additional cost
            var additionalDays = (newReturnDate - booking.ReturnDate).Days;
            var additionalCarCost = booking.Car.RentalPricePerDay * additionalDays;
            var additionalInsuranceCost = booking.Insurance != null ? 
                (additionalCarCost * booking.Insurance.CoveragePercentage / 100) : 0;
            
            var additionalCost = additionalCarCost + additionalInsuranceCost;

            // Update booking
            booking.ReturnDate = newReturnDate;
            booking.TotalPrice += additionalCost;
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Booking extended successfully. Additional cost: ${additionalCost:F2}";
            return RedirectToAction("FindBooking", new { bookingReference });
        }

        private string GenerateBookingReference()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"RG{timestamp}{random}";
        }

        private async Task<IActionResult> ReturnBookingView(Booking booking)
        {
            booking.Car = await _context.Cars.FindAsync(booking.CarID);
            ViewBag.Insurances = await _context.Insurances.ToListAsync();
            return View(booking);
        }
    }
}
