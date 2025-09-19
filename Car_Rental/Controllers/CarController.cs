using Car_Rental.Data;
using Car_Rental.Enum;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Car_Rental.Controllers
{
    public class CarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CarController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Car/List
        public async Task<IActionResult> List()
        {
            var cars = await _context.Cars.ToListAsync();
            return View(cars);
        }

        // GET: Car/Add
        [HttpGet]
        public IActionResult Add()
        {
            // "Booked" நிலையைத் தவிர்த்து, மற்ற நிலைகளை மட்டும் dropdown-ல் காண்பிக்க
            PopulateStatusViewBag();
            return View();
        }

        // POST: Car/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Car car, IFormFile? ImageFile)
        {
            // --- மாற்றம் 1: Any() என்பதற்கு பதிலாக AnyAsync() பயன்படுத்தப்பட்டுள்ளது ---
            if (await _context.Cars.AnyAsync(c => c.RegistrationNumber == car.RegistrationNumber))
            {
                ModelState.AddModelError("RegistrationNumber", "This registration number already exists.");
            }

            if (ModelState.IsValid)
            {
                if (ImageFile != null)
                {
                    car.ImageUrl = await UploadImage(ImageFile);
                }

                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(List));
            }

            // ModelState தவறாக இருந்தால், ViewBag-ஐ மீண்டும் நிரப்பவும்
            PopulateStatusViewBag();
            return View(car);
        }

        // GET: Car/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();
            return View(car);
        }

        // POST: Car/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Car car, IFormFile? ImageFile)
        {
            // --- மாற்றம் 2: ModelState சரிபார்ப்புக்குப் பிறகு, பிழை ஏற்பட்டால் ViewBag-ஐ நிரப்ப வேண்டும் ---
            if (!ModelState.IsValid)
            {
                // ஒருவேளை validation பிழை ஏற்பட்டால், பக்கத்தை மீண்டும் காண்பிக்கும் முன்
                // dropdown-களுக்கு தேவையான ViewBag-ஐ நிரப்ப வேண்டும்.
                return View(car);
            }

            var existingCar = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.CarId == car.CarId);
            if (existingCar == null) return NotFound();

            if (ImageFile != null)
            {
                if (!string.IsNullOrEmpty(existingCar.ImageUrl))
                {
                    DeleteImage(existingCar.ImageUrl);
                }
                car.ImageUrl = await UploadImage(ImageFile);
            }
            else
            {
                car.ImageUrl = existingCar.ImageUrl;
            }

            _context.Cars.Update(car);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(List));
        }

        // POST: Car/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            if (!string.IsNullOrEmpty(car.ImageUrl))
            {
                DeleteImage(car.ImageUrl);
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(List));
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int carId, DateTime pickupDate, DateTime returnDate)
        {
            var isBooked = await _context.Bookings
                .AnyAsync(b => b.CarID == carId &&
                               b.Status != BookingStatus.Cancelled &&
                               b.PickupDate < returnDate &&
                               b.ReturnDate > pickupDate);

            return Json(new { isAvailable = !isBooked });
        }


        // GET: Car/GetCarDetails/5
        [HttpGet]
        public async Task<IActionResult> GetCarDetails(int id)
        {
            var car = await _context.Cars
                .Where(c => c.CarId == id)
                .Select(c => new
                {
                    carId = c.CarId,
                    imageUrl = c.ImageUrl,
                    brand = c.Brand,
                    model = c.Model,
                    year = c.Year,
                    rentalPricePerDay = c.RentalPricePerDay,
                    offerPercentage = c.OfferPercentage,
                    offerAmount = c.OfferAmount,
                    fuelType = c.FuelType.ToString(),
                    transmission = c.Transmission.ToString(),
                    rating = c.Rating,
                    numberOfSeats = c.NumberOfSeats,
                    isAirConditioned = c.IsAirConditioned,
                    mileage = c.Mileage
                })
                .FirstOrDefaultAsync();

            if (car == null) return NotFound();
            return Json(car);
        }


        // GET: Car/GetStats
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var total = await _context.Cars.CountAsync();
            var available = await _context.Cars.CountAsync(c => c.Status == CarStatus.Available);
            var booked = await _context.Cars.CountAsync(c => c.Status == CarStatus.Booked);

            return Json(new { total, available, booked });
        }

        // --- Private Helper Methods ---
        private async Task<string> UploadImage(IFormFile file)
        {
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/cars");
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return "/images/cars/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            string imagePath = imageUrl.TrimStart('/');
            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        // --- மாற்றம் 3: ViewBag-ஐ நிரப்புவதற்கான ஒரு புதிய helper method ---
        private void PopulateStatusViewBag()
        {
            var statuses = System.Enum.GetValues(typeof(CarStatus))
                                     .Cast<CarStatus>()
                                     .Where(s => s != CarStatus.Booked);
            ViewBag.StatusList = new SelectList(statuses);
        }
    }
}