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
        private readonly ApplicationDbContext _context; // Assumes your DbContext is named ApplicationDbContext

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Booking(int Id, DateTime pickupDate, DateTime returnDate)
        {
            // 1. Retrieve the selected car from the database
            var selectedCar = await _context.Cars.FindAsync(Id);
            if (selectedCar == null)
            {
                return NotFound();
            }

            // 2. Retrieve all available insurances
            var insurances = await _context.Insurances.ToListAsync();
            ViewBag.Insurances = insurances;
            ViewBag.SelectedCar = selectedCar;

            // 3. Create a new booking model and pre-populate with data from the previous page
            var booking = new Booking
            {
                CarID = Id,
                PickupDate = pickupDate,
                ReturnDate = returnDate
            };

            // If user is authenticated, you can pre-populate other fields
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // You can add logic here to fetch user details and pre-populate the form
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booking(Booking booking, string guestFullName, string guestEmail, string guestPhoneNumber)
        {
            // --- Step 1: Handle User & Guest Logic ---
            if (User.Identity.IsAuthenticated)
            {
                // For a registered user, populate the UserID from their claims
                booking.UserID = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                booking.GuestID = null; // Ensure GuestID is null for registered users
            }
            else
            {
                // For a guest user, create a new Guest entity and save it
                var newGuest = new Guest
                {
                    FullName = guestFullName,
                    Email = guestEmail,
                    PhoneNumber = guestPhoneNumber
                };
                _context.Guests.Add(newGuest);
                await _context.SaveChangesAsync(); // Save to get the new GuestID
                booking.GuestID = newGuest.Id; // Assign the new GuestID to the booking
                booking.UserID = null;
            }

            // --- Step 2: Set Automatic Data ---
            booking.BookingDate = DateTime.Now;
            booking.BookingReference = GenerateBookingReference(); // Generate a unique reference number
            booking.Status = BookingStatus.pending;

            // --- Step 3: Check Model Validation and Final Availability ---
            if (!ModelState.IsValid)
            {
                // Reload data to display the form again with validation errors
                ViewBag.Insurances = await _context.Insurances.ToListAsync();
                ViewBag.SelectedCar = await _context.Cars.FindAsync(booking.CarID);
                return View(booking);
            }

            // A final check to prevent double-booking at the moment of submission
            var isCarAvailable = await IsCarAvailableAsync(booking.CarID, booking.PickupDate, booking.ReturnDate);
            if (!isCarAvailable)
            {
                ModelState.AddModelError("", "This car is no longer available for the selected dates. Please select another.");
                ViewBag.Insurances = await _context.Insurances.ToListAsync();
                ViewBag.SelectedCar = await _context.Cars.FindAsync(booking.CarID);
                return View(booking);
            }

            // --- Step 4: Save the Booking to the Database ---
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // --- Step 5: Redirect to Payment Page ---
            return RedirectToAction("Payment", new { bookingId = booking.BookingID });
        }

        // --- HELPER METHODS ---

        private string GenerateBookingReference()
        {
            // A simple method to generate a unique booking reference
            // You might use a more robust method in a production environment
            return "BOOK-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }

        private async Task<bool> IsCarAvailableAsync(int carId, DateTime pickupDate, DateTime returnDate)
        {
            // Check if there are any existing bookings for the same car
            // that overlap with the new booking dates.
            var existingBookings = await _context.Bookings
                .Where(b => b.CarID == carId &&
                            b.Status != BookingStatus.Cancelled && // Ignore cancelled bookings
                            (
                                (pickupDate >= b.PickupDate && pickupDate < b.ReturnDate) ||
                                (returnDate > b.PickupDate && returnDate <= b.ReturnDate) ||
                                (pickupDate <= b.PickupDate && returnDate >= b.ReturnDate)
                            ))
                .AnyAsync();

            return !existingBookings;
        }
    }
}