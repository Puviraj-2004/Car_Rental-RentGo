using Microsoft.AspNetCore.Mvc;

namespace Car_Rental.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult booking()
        {
            return View();
        }
    }
}
