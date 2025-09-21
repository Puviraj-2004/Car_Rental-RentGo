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

        // My Bookings - Show user bookings or search form
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Get authenticated user's bookings
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    var userBookings = await _context.Bookings
                        .Include(b => b.Car)
                        .Include(b => b.Insurance)
                        .Where(b => b.UserID == userId)
                        .OrderByDescending(b => b.BookingDate)
                        .ToListAsync();

                    return View(userBookings);
                }
            }

            // For guests, show search form
            return View(new List<Booking>());
        }

        // Find booking by reference code (handles both GET and POST)
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> FindBooking(string bookingReference = "")
        {
            if (string.IsNullOrEmpty(bookingReference))
            {
                if (Request.Method == "GET")
                {
                    return RedirectToAction("MyBookings");
                }
                else
                {
                    TempData["ErrorMessage"] = "Please enter a booking reference code.";
                    return RedirectToAction("MyBookings");
                }
            }

            return await GetBookingByReference(bookingReference);
        }

        private async Task<IActionResult> GetBookingByReference(string bookingReference)
        {
            if (string.IsNullOrEmpty(bookingReference))
            {
                TempData["ErrorMessage"] = "Please enter a booking reference code.";
                return RedirectToAction("MyBookings");
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
                return RedirectToAction("MyBookings");
            }

            return View("BookingDetails", booking);
        }

        // Cancel booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId, string bookingReference = "", string reason = "")
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Car)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    var errorMessage = "Booking not found.";
                    return Json(new { success = false, message = errorMessage });
                }

                // For authenticated users, check ownership
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId) && booking.UserID != int.Parse(userId))
                    {
                        var errorMessage = "You don't have permission to cancel this booking.";
                        return Json(new { success = false, message = errorMessage });
                    }
                }
                // For guests, check booking reference
                else if (!string.IsNullOrEmpty(bookingReference) && booking.BookingReference != bookingReference)
                {
                    var errorMessage = "Booking reference does not match.";
                    return Json(new { success = false, message = errorMessage });
                }

                if (booking.Status == BookingStatus.Cancelled)
                {
                    var errorMessage = "Booking is already cancelled.";
                    return Json(new { success = false, message = errorMessage });
                }

                // Check if booking can be cancelled (must be at least 24 hours before pickup)
                if (booking.PickupDate <= DateTime.Now.AddHours(24))
                {
                    var errorMessage = "Cannot cancel booking less than 24 hours before pickup time.";
                    return Json(new { success = false, message = errorMessage });
                }

                booking.Status = BookingStatus.Cancelled;

                // Update car status to available
                if (booking.Car != null)
                {
                    booking.Car.Status = CarStatus.Available;
                }

                await _context.SaveChangesAsync();

                var successMessage = "Booking cancelled successfully. You will receive a confirmation email shortly.";

                // Always return JSON for AJAX requests
                if (Request.Headers.ContainsKey("X-Requested-With") ||
                    Request.ContentType?.Contains("multipart/form-data") == true ||
                    Request.Method == "POST")
                {
                    return Json(new { success = true, message = successMessage });
                }

                TempData["SuccessMessage"] = successMessage;
                return RedirectToAction("MyBookings");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error cancelling booking: {ex.Message}";
                return Json(new { success = false, message = errorMessage });
            }
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

        // Check car availability for specific dates
        [HttpPost]
        public async Task<IActionResult> CheckAvailability()
        {
            try
            {
                var carId = int.Parse(Request.Form["carId"]);
                var pickupDate = DateTime.Parse(Request.Form["pickupDate"]);
                var returnDate = DateTime.Parse(Request.Form["returnDate"]);

                var isAvailable = await IsCarAvailable(carId, pickupDate, returnDate);
                return Json(new { isAvailable = isAvailable });
            }
            catch (Exception ex)
            {
                return Json(new { isAvailable = false, error = ex.Message });
            }
        }

        // Get available cars for specific dates
        [HttpPost]
        public async Task<IActionResult> GetAvailableCars()
        {
            try
            {
                var pickupDate = DateTime.Parse(Request.Form["pickupDate"]);
                var returnDate = DateTime.Parse(Request.Form["returnDate"]);

                var availableCars = await _context.Cars
                    .Where(c => c.Status == CarStatus.Available)
                    .ToListAsync();

                var availableCarsList = new List<object>();

                foreach (var car in availableCars)
                {
                    var isAvailable = await IsCarAvailable(car.CarId, pickupDate, returnDate);
                    if (isAvailable)
                    {
                        availableCarsList.Add(new
                        {
                            carId = car.CarId,
                            brand = car.Brand,
                            model = car.Model,
                            year = car.Year,
                            rentalPricePerDay = car.RentalPricePerDay,
                            imageUrl = car.ImageUrl ?? "/images/default-car.jpg"
                        });
                    }
                }

                return Json(new { cars = availableCarsList });
            }
            catch (Exception ex)
            {
                return Json(new { cars = new List<object>(), error = ex.Message });
            }
        }

        // Helper method to check if a car is available for given dates
        private async Task<bool> IsCarAvailable(int carId, DateTime pickupDate, DateTime returnDate)
        {
            var conflictingBookings = await _context.Bookings
                .Where(b => b.CarID == carId &&
                           b.Status != BookingStatus.Cancelled &&
                           b.PickupDate < returnDate &&
                           b.ReturnDate > pickupDate)
                .AnyAsync();

            return !conflictingBookings;
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

        // Admin Reservations Page
        public async Task<IActionResult> Booking(string search = "", string status = "", int page = 1)
        {
            var query = _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.User)
                .Include(b => b.Guest)
                .Include(b => b.Insurance)
                .AsQueryable();

            // Search functionality
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b =>
                    b.BookingReference.Contains(search) ||
                    b.Car.Brand.Contains(search) ||
                    b.Car.Model.Contains(search) ||
                    (b.User != null && b.User.FullName.Contains(search)) ||
                    (b.Guest != null && b.Guest.FullName.Contains(search)));
            }

            // Status filter
            if (!string.IsNullOrEmpty(status) && System.Enum.TryParse<BookingStatus>(status, out var bookingStatus))
            {
                query = query.Where(b => b.Status == bookingStatus);
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.pending);
            ViewBag.ActiveBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Booked);
            ViewBag.CompletedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Completed);

            return View(bookings);
        }

        // Cancel booking with confirmation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminCancelBooking(int bookingId, string reason = "")
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Car)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found." });
                }

                // Update booking status
                booking.Status = BookingStatus.Cancelled;

                // Update car status to available
                if (booking.Car != null)
                {
                    booking.Car.Status = CarStatus.Available;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Booking cancelled successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error cancelling booking: {ex.Message}" });
            }
        }

        // Get booking details for modal
        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.User)
                .Include(b => b.Guest)
                .Include(b => b.Insurance)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            var bookingDetails = new
            {
                bookingId = booking.BookingID,
                bookingReference = booking.BookingReference,
                status = booking.Status.ToString(),
                pickupDate = booking.PickupDate.ToString("MMM dd, yyyy"),
                returnDate = booking.ReturnDate.ToString("MMM dd, yyyy"),
                totalPrice = booking.TotalPrice,
                bookingDate = booking.BookingDate.ToString("MMM dd, yyyy HH:mm"),
                car = new
                {
                    brand = booking.Car?.Brand,
                    model = booking.Car?.Model,
                    year = booking.Car?.Year,
                    registrationNumber = booking.Car?.RegistrationNumber
                },
                customer = booking.User != null ? new
                {
                    name = booking.User.FullName,
                    email = booking.User.Email,
                    phone = booking.User.PhoneNumber,
                    type = "Registered"
                } : new
                {
                    name = booking.Guest?.FullName,
                    email = booking.Guest?.Email,
                    phone = booking.Guest?.PhoneNumber,
                    type = "Guest"
                },
                insurance = booking.Insurance?.Name ?? "No Insurance",
                paymentStatus = booking.Payments?.Any(p => p.Status == PaymentStatus.Paid) == true ? "Paid" : "Pending"
            };

            return Json(bookingDetails);
        }

    }
}
