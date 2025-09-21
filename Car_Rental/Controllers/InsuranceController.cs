using Car_Rental.Data;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Add this for async operations

namespace Car_Rental.Controllers
{
    public class InsuranceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InsuranceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Insurance/List
        // Shows the main list of all insurance policies.
        public async Task<IActionResult> List()
        {
            var insurances = await _context.Insurances.ToListAsync();
            return View(insurances);
        }

        // GET: /Insurance/Add
        // This action displays the form to add a new insurance policy.
        [HttpGet]
        public IActionResult Add()
        {
            // Simply returns the view for the "Add" form.
            return View();
        }

        // POST: /Insurance/Add
        // This action handles the form submission for adding a new insurance.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Insurance insurance)
        {
            if (ModelState.IsValid)
            {
                _context.Insurances.Add(insurance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(List));
            }
            // If the model is not valid, return the Add view with the submitted data
            // so the user can see validation errors and correct their input.
            return View(insurance);
        }

        // GET: /Insurance/Edit/5
        // This action displays the form to edit an existing insurance policy.
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insurance = await _context.Insurances.FindAsync(id);
            if (insurance == null)
            {
                return NotFound();
            }
            // Returns the Edit view, passing the specific insurance object to be edited.
            return View(insurance);
        }

        // POST: /Insurance/Edit/5
        // This action handles the form submission for editing an insurance.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Insurance insurance)
        {
            if (id != insurance.InsuranceID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(insurance);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Insurances.Any(e => e.InsuranceID == insurance.InsuranceID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(List));
            }
            // If the model is not valid, return the Edit view with the submitted data.
            return View(insurance);
        }

        // POST: /Insurance/Delete/5
        // This action handles the actual deletion of the insurance policy.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var insurance = await _context.Insurances.FindAsync(id);
            if (insurance != null)
            {
                _context.Insurances.Remove(insurance);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(List));
        }

        // GET: /Insurance/GetInsuranceDetails/5
        // This is a new API endpoint for your JavaScript fetch() call on the List page.
        // It returns insurance details in JSON format.
        [HttpGet]
        public async Task<IActionResult> GetInsuranceDetails(int id)
        {
            var insurance = await _context.Insurances.FindAsync(id);

            if (insurance == null)
            {
                return NotFound();
            }

            // Return the data as a JSON object, which JavaScript can easily understand.
            return Json(new
            {
                insuranceID = insurance.InsuranceID,
                name = insurance.Name,
                coveragePercentage = insurance.CoveragePercentage,
                description = insurance.Description
            });
        }
    }
}