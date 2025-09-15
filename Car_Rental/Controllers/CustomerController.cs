using Car_Rental.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public CustomerController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        [HttpGet]
        public IActionResult List()
        {
            var users = dbContext.Users.ToList();
            return View(users);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int Id)
        {
            var user = dbContext.Users.Find(Id);
            if (user == null) return NotFound();

            dbContext.Users.Remove(user);
            dbContext.SaveChanges();
            return RedirectToAction(nameof(List));
        }

        public IActionResult Details(int Id)
        {
            var user = dbContext.Users.Find(Id);
            if (user == null) return NotFound();
            return View(user);
        }

    }
}
