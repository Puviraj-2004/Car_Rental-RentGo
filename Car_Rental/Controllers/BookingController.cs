using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using BCrypt.Net;

namespace Car_Rental.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context) { _context = context; }

        private static string GenerateBookingReference()
        {
            var random = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var randomPart = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
            return $"BK-{DateTime.UtcNow:yyMMdd}-{randomPart}";
        }

        // GET Action: Shows the initial booking page.
        [HttpGet]
        public async Task<IActionResult> Booking(int id, DateTime? pickupDate, DateTime? returnDate)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                TempData["ErrorMessage"] = "The selected car could not be found.";
                return RedirectToAction("Cars", "Guest");
            }

            ViewBag.SelectedCar = car;
            ViewBag.Insurances = await _context.Insurances.ToListAsync();
            ViewBag.Drivers = await _context.Drivers.Where(d => d.Status == DriverStatus.Available).ToListAsync();

            var bookingModel = new Booking
            {
                CarID = car.CarId,
                PickupDate = pickupDate ?? DateTime.Today,
                ReturnDate = returnDate ?? DateTime.Today.AddDays(1)
            };
            return View(bookingModel);
        }

        // ==========================================================
        //         ACTION 1: THE "GATEKEEPER" (NEW LOGIC)
        // This action is called first when the user clicks "Continue".
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProceedToAuthentication(Booking booking)
        {
            // First, validate the rental details (dates, license, etc.)
            if (!ModelState.IsValid)
            {
                // If there's an error, redisplay the form with the errors.
                ViewBag.SelectedCar = _context.Cars.Find(booking.CarID);
                ViewBag.Insurances = _context.Insurances.ToList();
                ViewBag.Drivers = _context.Drivers.Where(d => d.Status == DriverStatus.Available).ToList();
                return View("Booking", booking);
            }

            // Temporarily store the valid booking details in the session.
            TempData["PendingBooking"] = JsonSerializer.Serialize(booking);

            if (User.Identity.IsAuthenticated)
            {
                // If user is already logged in, they can skip the details step.
                return RedirectToAction("FinalizeBooking");
            }
            else
            {
                // If user is a guest, send them to the page to enter their details.
                return RedirectToAction("GuestDetails");
            }
        }

        // ==========================================================
        //       ACTION 2: SHOW THE GUEST DETAILS PAGE (NEW LOGIC)
        // ==========================================================
        [HttpGet]
        public IActionResult GuestDetails()
        {
            if (!TempData.ContainsKey("PendingBooking"))
            {
                // Protect this page from being accessed directly.
                return RedirectToAction("Home", "Guest");
            }
            return View();
        }

        // ==========================================================
        //       ACTION 3: FINALIZE THE BOOKING (NEW LOGIC)
        // This is the final step that creates all records.
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeBooking(string guestFullName, string guestEmail, string guestPhoneNumber)
        {
            var bookingJson = TempData["PendingBooking"] as string;
            if (string.IsNullOrEmpty(bookingJson))
            {
                return RedirectToAction("Home", "Guest"); // Safety check
            }
            var booking = JsonSerializer.Deserialize<Booking>(bookingJson);

            // Find or Create the User based on the guest details provided.
            User bookingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == guestEmail);
            if (bookingUser == null)
            {
                bookingUser = new User
                {
                    FullName = guestFullName,
                    Email = guestEmail,
                    PhoneNumber = guestPhoneNumber,
                    Role = "User",
                    Password = BCrypt.Net.BCrypt.HashPassword(Path.GetRandomFileName()),
                    MustChangePassword = true
                };
                _context.Users.Add(bookingUser);
            }

            // Now that we have a user, we can finalize the booking.
            booking.UserID = bookingUser.UserID;
            return await CreateBookingAndInvoice(booking);
        }

        // This private helper is called when a logged-in user skips the guest details page.
        [HttpGet]
        public async Task<IActionResult> FinalizeBooking()
        {
            if (!User.Identity.IsAuthenticated) return Unauthorized();
            var bookingJson = TempData["PendingBooking"] as string;
            if (string.IsNullOrEmpty(bookingJson)) return RedirectToAction("Home", "Guest");

            var booking = JsonSerializer.Deserialize<Booking>(bookingJson);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            booking.UserID = userId;

            return await CreateBookingAndInvoice(booking);
        }

        // --- SHARED PRIVATE HELPER FOR CREATING RECORDS ---
        private async Task<IActionResult> CreateBookingAndInvoice(Booking booking)
        {
            var car = await _context.Cars.FindAsync(booking.CarID);
            if (car == null || car.Status != CarStatus.Available)
            {
                TempData["ErrorMessage"] = "Sorry, this car was booked while you were entering your details.";
                return RedirectToAction("Cars", "Guest");
            }

            booking.BookingReference = GenerateBookingReference();
            booking.BookingDate = DateTime.UtcNow;
            booking.Status = BookingStatus.pending;

            int numberOfDays = (booking.ReturnDate - booking.PickupDate).Days > 0 ? (booking.ReturnDate - booking.PickupDate).Days : 1;
            booking.TotalPrice = numberOfDays * car.RentalPricePerDay; // Add your insurance/driver fee logic here

            _context.Bookings.Add(booking);
            car.Status = CarStatus.Booked;

            var invoice = new Invoice
            {
                Booking = booking,
                InvoiceDate = DateTime.UtcNow,
                Subtotal = booking.TotalPrice,
                TotalAmount = booking.TotalPrice,
                IsPaid = false
            };
            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            return RedirectToAction("Pay", "Invoice", new { reference = booking.BookingReference });
        }
    }
}