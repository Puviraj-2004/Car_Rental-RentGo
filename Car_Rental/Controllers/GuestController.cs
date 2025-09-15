using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Car_Rental.Controllers
{
    public class GuestController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public GuestController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Displays cars and handles filtering based on selected dates.
        /// Rule 1: On initial load (no dates), shows all cars.
        /// Rule 2: When dates are provided, filters cars.
        /// Rule 3: Shows only cars that are 'Available' and not booked in the selected range.
        /// </summary>
        /// <param name="pickupDate">The start date for the rental period.</param>
        /// <param name="returnDate">The end date for the rental period.</param>
        /// <returns>A view with a list of cars.</returns>
        public async Task<IActionResult> Home(DateTime? pickupDate, DateTime? returnDate)
        {
            // Niyamam 1: Webpage load aagum pothu allathu thethigal illavittal
            if (pickupDate == null || returnDate == null)
            {
                // Database-il ulla anaithu car-galaiyum kaattavum
                var allCars = await dbContext.Cars.ToListAsync();
                return View(allCars);
            }

            // Niyamam 2: Thedal thodangugirathu, muthalil thethigalai validate seithal
            if (pickupDate < DateTime.Today)
            {
                ModelState.AddModelError("PickupDate", "Pick-up date must be today or a future date.");
            }

            if (returnDate <= pickupDate)
            {
                ModelState.AddModelError("ReturnDate", "Return date must be after the pick-up date.");
            }

            // Validation-il thavaru irunthal, oru verum list-ai anuppavum (pilai seithi view-il kaattappadum)
            if (!ModelState.IsValid)
            {
                ViewBag.ValidationFailed = true; // View-il intha flag-ai payanpaduthi pilai seithi kaattalam
                return View(new List<Car>()); // Empty list anuppappadugirathu
            }

            // Niyamam 3 & 4: Sariyana filtering seyalmurai

            // A. Thedal kaalathil porunthum (overlapping) booking-kalai konda car-galin ID-kalai eduthal
            var bookedCarIds = await dbContext.Bookings
                .Where(b => b.PickupDate < returnDate.Value && b.ReturnDate > pickupDate.Value)
                .Select(b => b.CarID)
                .Distinct()
                .ToListAsync();

            // B. Sariyana 'Available' car-galai thedi eduthal
            //    Niyamam: Car-in status 'Available' aaga irukka vendum, MELUM antha car 'bookedCarIds' list-il irukka koodathu.
            var availableCars = await dbContext.Cars
                .Where(c =>
                    c.Status == CarStatus.Available &&
                    !bookedCarIds.Contains(c.CarId))
                .ToListAsync();

            // C. Mudivana, filter seiyyapatta list-ai View-ku anuppavum
            return View(availableCars);
        }
    }
}