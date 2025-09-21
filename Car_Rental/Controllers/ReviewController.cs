using Car_Rental.Data;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Car_Rental.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int bookingId, int carId, int rating, string? comment)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    TempData["ErrorMessage"] = "You must be logged in to submit a review.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int userId))
                {
                    TempData["ErrorMessage"] = "Invalid user session.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Verify booking belongs to user and is completed
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId &&
                                            b.UserID == userId &&
                                            b.Status == Car_Rental.Enum.BookingStatus.Completed);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Invalid booking or booking not completed.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Check if review already exists
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.BookingId == bookingId && r.UserId == userId);

                if (existingReview != null)
                {
                    TempData["ErrorMessage"] = "You have already reviewed this booking.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Create new review
                var review = new Review
                {
                    UserId = userId,
                    CarId = carId,
                    BookingId = bookingId,
                    Rating = rating,
                    Comment = comment?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for your review!";
                return RedirectToAction("MyBookings", "Booking");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error submitting review: {ex.Message}";
                return RedirectToAction("MyBookings", "Booking");
            }
        }

        // Get reviews for a car
        [HttpGet]
        public async Task<IActionResult> GetCarReviews(int carId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.CarId == carId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    UserName = r.User.FullName
                })
                .ToListAsync();

            return Json(reviews);
        }

        // Get average rating for a car
        [HttpGet]
        public async Task<IActionResult> GetCarRating(int carId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.CarId == carId)
                .ToListAsync();

            if (!reviews.Any())
            {
                return Json(new { averageRating = 0, totalReviews = 0 });
            }

            var averageRating = Math.Round(reviews.Average(r => r.Rating), 1);
            var totalReviews = reviews.Count;

            return Json(new { averageRating, totalReviews });
        }
    }
}
