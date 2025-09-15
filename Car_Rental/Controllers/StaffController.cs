using Car_Rental.Data;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult List()
        {
            var staffs = _context.Staffs.ToList();
            return View(staffs);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Staff staff, IFormFile PhotoFile)
        {
            if (ModelState.IsValid)
            {
                // Handle photo upload
                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/staffs");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(PhotoFile.FileName);
                    var filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        PhotoFile.CopyTo(stream);
                    }

                    // Save relative path in DB
                    staff.PhotoUrl = "/images/staffs/" + fileName;
                }
                else
                {
                    // Default image if no upload
                    staff.PhotoUrl = "/images/default-staff.png";
                }

                // Set default hire date
                if (staff.HireDate == default)
                    staff.HireDate = DateTime.Now;

                _context.Staffs.Add(staff);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Staff member added successfully!";
                return RedirectToAction("List");
            }

            return View(staff);
        }



        [HttpGet]
        public IActionResult Edit(int id)
        {
            var staff = _context.Staffs.Find(id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Staff staff, IFormFile? PhotoFile)
        {
            if (ModelState.IsValid)
            {
                var existingStaff = _context.Staffs.AsNoTracking().FirstOrDefault(s => s.StaffId == staff.StaffId);
                if (existingStaff == null) return NotFound();

                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/staffs");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(PhotoFile.FileName);
                    var filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        PhotoFile.CopyTo(stream);
                    }

                    staff.PhotoUrl = "/images/staffs/" + fileName;
                }
                else
                {
                    staff.PhotoUrl = existingStaff.PhotoUrl;
                }

                _context.Staffs.Update(staff);
                _context.SaveChanges();
                return RedirectToAction(nameof(List));
            }
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var staff = _context.Staffs.Find(id);
            if (staff == null) return NotFound();

            _context.Staffs.Remove(staff);
            _context.SaveChanges();
            return RedirectToAction(nameof(List));
        }

        public IActionResult Details(int id)
        {
            var staff = _context.Staffs.Find(id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        [HttpGet]
        public IActionResult GetStaffDetails(int id)
        {
            var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == id);
            if (staff == null) return NotFound();

            return Json(new
            {
                staffId = staff.StaffId,
                fullName = staff.FullName,
                email = staff.Email,
                phoneNumber = staff.PhoneNumber,
                role = staff.Role,
                isActive = staff.IsActive,
                hireDate = staff.HireDate.ToString("yyyy-MM-dd"),
                salary = staff.Salary,
                address = staff.Address,
                photoUrl = staff.PhotoUrl
            });
        }

        [HttpGet]
        public IActionResult GetStats()
        {
            var total = _context.Staffs.Count();
            var active = _context.Staffs.Count(s => s.IsActive);
            var inactive = _context.Staffs.Count(s => !s.IsActive);

            return Json(new { total, active, inactive });
        }
    }
}
