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
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static string GenerateBookingReference()
        {
            var random = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var randomPart = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
            return $"BK-{DateTime.UtcNow:yyMMdd}-{randomPart}";
        }

        // GET: /Booking/Booking/5?pickupDate=...
        [HttpGet]
        public async Task<IActionResult> Booking(int id, DateTime? pickupDate, DateTime? returnDate)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null || car.Status != CarStatus.Available)
            {
                TempData["ErrorMessage"] = "Sorry, this car is not available for booking.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Booking(Booking booking, string guestFullName, string guestEmail, string guestPhoneNumber)
        {
            ModelState.Remove("TotalPrice");
            ModelState.Remove("BookingReference");
            ModelState.Remove("Status");
            ModelState.Remove("User");

            if (!ModelState.IsValid)
            {
                ViewBag.SelectedCar = await _context.Cars.FindAsync(booking.CarID);
                ViewBag.Insurances = await _context.Insurances.ToListAsync();
                ViewBag.Drivers = await _context.Drivers.Where(d => d.Status == DriverStatus.Available).ToListAsync();
                return View(booking);
            }

            User bookingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == guestEmail);
            if (bookingUser == null)
            {
                bookingUser = new User
                {
                    FullName = guestFullName,
                    Email = guestEmail,
                    PhoneNumber = guestPhoneNumber,
                    Role = "User",
                    Password = BCrypt.Net.BCrypt.HashPassword(Path.GetRandomFileName()), // Secure temporary password
                    MustChangePassword = true
                };
                _context.Users.Add(bookingUser);
                await _context.SaveChangesAsync();
            }
            booking.UserID = bookingUser.UserID;

            var car = await _context.Cars.FindAsync(booking.CarID);
            if (car == null || car.Status != CarStatus.Available)
            {
                TempData["ErrorMessage"] = "Sorry, this car has just been booked and is no longer available.";
                return RedirectToAction("Cars", "Guest");
            }

            int numberOfDays = (booking.ReturnDate - booking.PickupDate).Days;
            if (numberOfDays <= 0) numberOfDays = 1;
            decimal rentalTotal = numberOfDays * car.RentalPricePerDay;
            // Add insurance and driver fee logic here if needed
            booking.TotalPrice = rentalTotal;

            booking.BookingReference = GenerateBookingReference();
            booking.BookingDate = DateTime.UtcNow;
            booking.Status = BookingStatus.Confirmed;

            car.Status = CarStatus.Booked;
            if (booking.DriverID.HasValue)
            {
                var driver = await _context.Drivers.FindAsync(booking.DriverID);
                if (driver != null) driver.Status = DriverStatus.Unavailable;
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction("Confirmation", new { reference = booking.BookingReference });
        }

        public IActionResult Confirmation(string reference)
        {
            ViewBag.BookingReference = reference;
            return View();
        }
    }
}