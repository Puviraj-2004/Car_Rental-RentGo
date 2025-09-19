using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // DateTime-க்கு தேவை
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace Car_Rental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AdminController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId)) return RedirectToAction("Logout", "User");

                var adminUser = await _context.Users.FindAsync(int.Parse(adminId));
                if (adminUser == null) return RedirectToAction("Logout", "User");

                // --- புள்ளிவிவரங்களைக் கணக்கிடுதல் ---

                // 1. Most Booked Vehicle
                var mostBookedCarInfo = await _context.Bookings
                    .GroupBy(b => b.CarID)
                    .Select(g => new { CarId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefaultAsync();

                string mostBookedVehicle = "No Bookings Yet";
                if (mostBookedCarInfo != null)
                {
                    var car = await _context.Cars.FindAsync(mostBookedCarInfo.CarId);
                    mostBookedVehicle = car != null ? $"{car.Brand} {car.Model}" : "Vehicle Not Found";
                }

                // 2. Most Rated Vehicle
                string mostRatedVehicle = "No Reviews Yet";
                if (await _context.Reviews.AnyAsync())
                {
                    var mostRatedCarInfo = await _context.Reviews
                        .GroupBy(r => r.CarId)
                        .Select(g => new { CarId = g.Key, AverageRating = g.Average(r => r.Rating) })
                        .OrderByDescending(x => x.AverageRating)
                        .FirstOrDefaultAsync();

                    if (mostRatedCarInfo != null)
                    {
                        var car = await _context.Cars.FindAsync(mostRatedCarInfo.CarId);
                        mostRatedVehicle = car != null ? $"{car.Brand} {car.Model}" : "Vehicle Not Found";
                    }
                }

                // 3. INVOICE TABLE-ஐப் பயன்படுத்தி வருமானக் கணக்கீடுகள்
                var totalRevenue = await _context.Invoices.Where(i => i.IsPaid).SumAsync(i => i.TotalAmount);
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var currentMonthRevenue = await _context.Invoices
                    .Where(i => i.IsPaid && i.InvoiceDate >= startOfMonth)
                    .SumAsync(i => i.TotalAmount);

                // 4. நான்கு கார் நிலைகளுக்கும் தனித்தனியாக கணக்கீடு
                var availableCarsCount = await _context.Cars.CountAsync(c => c.Status == CarStatus.Available);
                var bookedCarsCount = await _context.Cars.CountAsync(c => c.Status == CarStatus.Booked);
                var maintenanceCarsCount = await _context.Cars.CountAsync(c => c.Status == CarStatus.UnderMaintenance);
                var notAvailableCarsCount = await _context.Cars.CountAsync(c => c.Status == CarStatus.NotAvailable);

                // --- Model-ஐ உருவாக்குதல் ---
                var model = new
                {
                    TotalCars = await _context.Cars.CountAsync(),
                    TotalBookings = await _context.Bookings.CountAsync(),
                    ActiveCustomers = await _context.Users.CountAsync(u => u.Role == "Customer"),
                    MustChangePassword = adminUser.MustChangePassword,
                    MostBookedVehicle = mostBookedVehicle,
                    MostRatedVehicle = mostRatedVehicle,

                    // வருமானப் புள்ளிவிவரங்கள்
                    TotalRevenue = totalRevenue,
                    CurrentMonthRevenue = currentMonthRevenue,

                    // நான்கு கார் நிலைகள்
                    AvailableCars = availableCarsCount,
                    BookedCars = bookedCarsCount,
                    MaintenanceCars = maintenanceCarsCount,
                    NotAvailableCars = notAvailableCarsCount
                };

                return View(model);
            }
            catch (Exception)
            {
                return View("Error"); // பிழை ஏற்பட்டால் Error பக்கத்திற்கு அனுப்பவும்
            }
        }

        // === API Endpoints for Charts ===

        [HttpGet]
        public async Task<JsonResult> GetRevenueTrends()
        {
            var revenueData = await _context.Invoices
                .Where(i => i.IsPaid && i.InvoiceDate > DateTime.Now.AddMonths(-12))
                .GroupBy(i => new { Year = i.InvoiceDate.Year, Month = i.InvoiceDate.Month })
                .Select(g => new { Date = new DateTime(g.Key.Year, g.Key.Month, 1), Total = g.Sum(i => i.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var labels = revenueData.Select(r => r.Date.ToString("MMM yy")).ToArray();
            var data = revenueData.Select(r => r.Total).ToArray();

            return Json(new { labels, data });
        }

        [HttpGet]
        public async Task<JsonResult> GetBookingTrends()
        {
            var bookingsData = await _context.Bookings
                .Where(b => b.BookingDate > DateTime.Now.AddMonths(-12))
                .GroupBy(b => new { Year = b.BookingDate.Year, Month = b.BookingDate.Month })
                .Select(g => new { Date = new DateTime(g.Key.Year, g.Key.Month, 1), Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var labels = bookingsData.Select(b => b.Date.ToString("MMM yy")).ToArray();
            var data = bookingsData.Select(b => b.Count).ToArray();

            return Json(new { labels, data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Password must be at least 6 characters long." });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "The new password and confirmation password do not match." });
            }

            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdString))
            {
                return Json(new { success = false, message = "Authentication error. User ID not found." });
            }

            var adminUser = await _context.Users.FindAsync(int.Parse(adminIdString));
            if (adminUser != null)
            {
                adminUser.Password = _passwordHasher.HashPassword(adminUser, newPassword);
                adminUser.MustChangePassword = false;

                await _context.SaveChangesAsync(); // SaveChangesAsync ஆக மாற்றப்பட்டது

                return Json(new { success = true, message = "Password updated successfully!" });
            }

            return Json(new { success = false, message = "An error occurred. Admin user could not be found." });
        }
    }
}