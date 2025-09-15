using Car_Rental.Data;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Controllers
{
    public class DiscountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DiscountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Discount/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: Discount/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(DiscountCode discount)
        {
            if (_context.DiscountCodes.Any(d => d.Code == discount.Code))
            {
                ModelState.AddModelError("Code", "This discount code already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.DiscountCodes.Add(discount);
                await _context.SaveChangesAsync();
                return RedirectToAction("List");
            }

            return View(discount);
        }

        // GET: Discount/List
        public async Task<IActionResult> List(string searchTerm, string status)
        {
            var discounts = _context.DiscountCodes.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                discounts = discounts.Where(d => d.Code.Contains(searchTerm));

            var today = DateTime.Now;
            if (!string.IsNullOrEmpty(status))
            {
                discounts = discounts.Where(d =>
                    (status == "Inactive" && !d.IsActive) ||
                    (status == "Expired" && d.EndDate < today && d.IsActive) ||
                    (status == "Upcoming" && d.StartDate > today && d.IsActive) ||
                    (status == "Valid" && d.StartDate <= today && d.EndDate >= today && d.IsActive)
                );
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;

            return View(await discounts.ToListAsync());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var discount = await _context.DiscountCodes.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            _context.DiscountCodes.Remove(discount);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(List));
        }


        // GET: Discount/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var discount = await _context.DiscountCodes.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }
            return View(discount);
        }

        // POST: Discount/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DiscountCode discount)
        {
            if (id != discount.DiscountCodeId)
            {
                return BadRequest();
            }

            // Code uniqueness check
            if (_context.DiscountCodes.Any(d => d.Code == discount.Code && d.DiscountCodeId != id))
            {
                ModelState.AddModelError("Code", "This discount code already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(discount);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(List));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again later.");
                }
            }

            return View(discount);
        }

    }
}
