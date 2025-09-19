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
        private readonly ApplicationDbContext _context;

        public GuestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Home()
        {
            var allCars = await _context.Cars
                .OrderByDescending(c => c.Rating) // rating order same-a வைத்துக்கலாம்
                .Take(6) // still only 6 cars
                .ToListAsync();

            return View(allCars);
        }


        // =========================================================================
        //          THIS IS THE FINAL, PERFECT VERSION OF YOUR CARS ACTION
        // =========================================================================
        public async Task<IActionResult> Cars(DateTime? pickupDate, DateTime? returnDate)
        {
            ViewBag.PickupDate = pickupDate;
            ViewBag.ReturnDate = returnDate;

            List<Car> finalCarsList;

            // --- SCENARIO 2: USER HAS SELECTED DATES ---
            if (pickupDate.HasValue && returnDate.HasValue)
            {
                ViewBag.IsFiltered = true; // Tell the view we are showing filtered results

                if (returnDate <= pickupDate)
                {
                    TempData["ErrorMessage"] = "Return date must be after the pickup date.";
                    return View(new List<Car>());
                }

                // Find unavailable car IDs for the selected period
                var unavailableCarIds = await _context.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled &&
                                b.PickupDate < returnDate.Value &&
                                b.ReturnDate > pickupDate.Value)
                    .Select(b => b.CarID)
                    .Distinct()
                    .ToListAsync();

                // Get ONLY the cars that are generally rentable AND are not in the unavailable list
                finalCarsList = await _context.Cars
                    .Where(c => c.Status != CarStatus.NotAvailable && c.Status != CarStatus.UnderMaintenance)
                    .Where(c => !unavailableCarIds.Contains(c.CarId))
                    .OrderByDescending(c => c.Rating)
                    .ToListAsync();
            }
            // --- SCENARIO 1: USER IS JUST BROWSING (NO DATES) ---
            else
            {
                ViewBag.IsFiltered = false; // Tell the view we are showing the full catalog

                // Get EVERYTHING from the database to show the full catalog
                finalCarsList = await _context.Cars
                    .OrderByDescending(c => c.Rating)
                    .ToListAsync();
            }

            return View(finalCarsList);
        }
    }
}