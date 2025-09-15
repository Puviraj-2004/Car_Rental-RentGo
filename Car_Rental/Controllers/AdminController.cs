using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [HttpGet]
        public IActionResult Dashboard()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminUser = _context.Users.Find(int.Parse(adminId));

            if (adminUser == null)
            {
                return RedirectToAction("Logout", "User");

            }

            var allCars = _context.Cars.ToList();
            var allBookings = _context.Bookings.ToList();
            var allUsers = _context.Users.ToList();

            var model = new
            {
                TotalCars = allCars.Count,
                AvailableCars = allCars.Count(c => c.Status == CarStatus.Available),
                TotalBookings = allBookings.Count,
                ActiveCustomers = allUsers.Count(u => u.Role == "Customer"),
                MustChangePassword = adminUser.MustChangePassword
            };

            return View(model);
        }

        // =================================================================
        //      PUTHIYA ACTION (VIEWMODEL ILLAMAL)
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePassword(string newPassword, string confirmPassword)
        {
            // Basic validation
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Password must be at least 6 characters long." });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "The new password and confirmation password do not match." });
            }

            // Get the current user's ID from their claims
            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdString))
            {
                return Json(new { success = false, message = "Authentication error. User ID not found." });
            }

            var adminUser = _context.Users.Find(int.Parse(adminIdString));

            if (adminUser != null)
            {
                // ==============================================================
                //                      THE FIX IS HERE
                // ==============================================================
                // Hash the new password before saving it to the database.
                adminUser.Password = _passwordHasher.HashPassword(adminUser, newPassword);
                // ==============================================================

                // Set the flag to false since the password has now been changed
                adminUser.MustChangePassword = false;

                _context.SaveChanges();

                // Let the client know the update was successful
                return Json(new { success = true, message = "Password updated successfully!" });
            }

            return Json(new { success = false, message = "An error occurred. Admin user could not be found." });
        }
    }
}