using Car_Rental.Data;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Controllers
{
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DriverController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> List()
        {
            var drivers = await _context.Drivers.ToListAsync();
            return View(drivers);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Driver driver, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
                return View(driver);

            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Create folder path
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/drivers");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Unique file name
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                // Save file to wwwroot/images/drivers/
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Save relative path in DB
                driver.PhotoUrl = "/images/drivers/" + fileName;
            }
            else
            {
                // If no image uploaded, provide a default image
                driver.PhotoUrl = "/images/default.png";
            }

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync(); // Use async save
            return RedirectToAction(nameof(List));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var driver = _context.Drivers.Find(id);
            if (driver == null) return NotFound();
            return View(driver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Driver driver, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                var existingDriver = _context.Drivers.AsNoTracking().FirstOrDefault(d => d.DriverID == driver.DriverID);
                if (existingDriver == null) return NotFound();

                // If user uploads a new image
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/drivers");

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageFile.CopyTo(stream);
                    }

                    driver.PhotoUrl = "/images/drivers/" + fileName;
                }
                else
                {
                    // Keep old image
                    driver.PhotoUrl = existingDriver.PhotoUrl;
                }

                _context.Drivers.Update(driver);
                _context.SaveChanges();
                return RedirectToAction(nameof(List));
            }
            return View(driver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var driver = _context.Drivers.Find(id);
            if (driver == null) return NotFound();

            _context.Drivers.Remove(driver);
            _context.SaveChanges();
            return RedirectToAction(nameof(List));
        }

        // JSON API for frontend details pane
        [HttpGet]
        public IActionResult GetDriverDetails(int id)
        {
            var driver = _context.Drivers
                .Where(d => d.DriverID == id)
                .Select(d => new
                {
                    driverID = d.DriverID,
                    fullName = d.FullName,
                    nic = d.NIC,
                    driverLicenseNumber = d.DriverLicenseNumber,
                    licenseExpiryDate = d.LicenseExpiryDate.ToString("yyyy-MM-dd"),
                    status = d.Status.ToString(),
                    photoUrl = d.PhotoUrl,
                    feePerDay = d.FeePerDay // <-- இந்த வரி புதிதாக சேர்க்கப்பட்டுள்ளது
                })
                .FirstOrDefault();

            if (driver == null)
            {
                return NotFound();
            }

            return Json(driver);
        }
        [HttpGet]
        public IActionResult GetStats()
        {
            var total = _context.Drivers.Count();
            return Json(new { total });
        }
    }
}
