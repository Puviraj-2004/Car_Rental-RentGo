using Car_Rental.Data;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Car_Rental.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user, string confirmPassword)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            if (user.Password != confirmPassword)
            {
                ViewBag.PasswordError = "❌ The password and confirmation password do not match.";
                return View(user);
            }

            user.Password = _passwordHasher.HashPassword(user, user.Password);

            user.Role = "Customer";
            user.MustChangePassword = false;

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"✅ Registration successful! Please log in.";
            return RedirectToAction("Login");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe = false)
        {
            var user = _context.Users.FirstOrDefault(u => (u.Email == username || u.PhoneNumber == username));

            if (user != null && _passwordHasher.VerifyHashedPassword(user, user.Password,
                password) == PasswordVerificationResult.Success)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString())
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties { IsPersistent = rememberMe }
                );

                return user.Role switch
                {
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    _ => RedirectToAction("Home", "Guest"),
                };
            }

            ViewBag.ErrorMessage = "❌ Invalid email/phone or password.";
            return View();
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(string username, string password, string returnUrl = null, bool rememberMe = false)
        //{
        //    var user = _context.Users.FirstOrDefault(u => (u.Email == username || u.PhoneNumber == username));

        //    if (user != null && _passwordHasher.VerifyHashedPassword(user, user.Password, password) == PasswordVerificationResult.Success)
        //    {
        //        // ... (your existing code for creating claims and signing in)

        //        // THE UPGRADED REDIRECT LOGIC
        //        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        //        {
        //            return Redirect(returnUrl);
        //        }
        //        else
        //        {
        //            return user.Role switch
        //            {
        //                "Admin" => RedirectToAction("Dashboard", "Admin"),
        //                _ => RedirectToAction("Home", "Guest"),
        //            };
        //        }
        //    }
        //    ViewBag.ErrorMessage = "Invalid credentials.";
        //    ViewData["ReturnUrl"] = returnUrl;
        //    return View();
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Home", "Guest");
        }
    }
}