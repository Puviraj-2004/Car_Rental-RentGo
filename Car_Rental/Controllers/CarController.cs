using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Controllers
{
    public class CarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show all cars

        
        public IActionResult List()
        {
            var cars = _context.Cars.ToList();
            return View(cars); 
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Car car, IFormFile ImageFile)
        {
            if (_context.Cars.Any(c => c.RegistrationNumber == car.RegistrationNumber))
            {
                ModelState.AddModelError("RegistrationNumber", "This registration number already exists.");
            }
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Create folder path
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/cars");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    // Unique file name
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(folderPath, fileName);

                    // Save file to wwwroot/images/cars/
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageFile.CopyTo(stream);
                    }

                    // Save relative path in DB
                    car.ImageUrl = "/images/cars/" + fileName;
                }

                _context.Cars.Add(car);
                _context.SaveChanges();
                return RedirectToAction(nameof(List));
            }
            return View(car);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var car = _context.Cars.Find(id);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Car car, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                var existingCar = _context.Cars.AsNoTracking().FirstOrDefault(c => c.CarId == car.CarId);
                if (existingCar == null) return NotFound();

                // If user uploads a new image
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/cars");

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageFile.CopyTo(stream);
                    }

                    car.ImageUrl = "/images/cars/" + fileName;
                }
                else
                {
                    // Keep old image
                    car.ImageUrl = existingCar.ImageUrl;
                }

                _context.Cars.Update(car);
                _context.SaveChanges();
                return RedirectToAction(nameof(List));
            }
            return View(car);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var car = _context.Cars.Find(id);
            if (car == null) return NotFound();

            _context.Cars.Remove(car);
            _context.SaveChanges();
            return RedirectToAction(nameof(List));
        }

        public IActionResult Details(int id)
        {
            var car = _context.Cars.Find(id);
            if (car == null) return NotFound();
            return View(car);
        }

        [HttpGet]
        public IActionResult GetCarDetails(int id)
        {
            var car = _context.Cars.FirstOrDefault(c => c.CarId == id);
            if (car == null) return NotFound();

            return Json(new
            {
                carId = car.CarId,
                registrationNumber = car.RegistrationNumber,
                brand = car.Brand,
                model = car.Model,
                year = car.Year,
                rentalPricePerDay = car.RentalPricePerDay,
                fuelType = car.FuelType.ToString(),
                transmission = car.Transmission.ToString(),
                status = car.Status.ToString(),
                imageUrl = car.ImageUrl,
                offerPercentage = car.OfferPercentage,
                offerAmount = car.OfferAmount
            });
        }

        [HttpGet]
        public IActionResult GetStats()
        {
            var total = _context.Cars.Count();
            var available = _context.Cars.Count(c => c.Status == CarStatus.Available);
            var booked = _context.Cars.Count(c => c.Status == CarStatus.Booked);

            return Json(new { total, available, booked });
        }
    }
}
