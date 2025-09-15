using Car_Rental.Data;
using Car_Rental.Models.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Car_Rental
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // Add DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("CarRentalDbConnection")));

            // ✅ Add Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/User/Login";
                options.LogoutPath = "/User/Logout";
                options.Cookie.Name = "CarRentalAuth";
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            });

            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            var app = builder.Build();



            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

                    // Admin user aagave illainna mattum create pannurom
                    if (!context.Users.Any(u => u.Email == "admin@rentgo.com"))
                    {
                        var adminUser = new User
                        {
                            FullName = "Administrator",
                            Email = "admin@rentgo.com",
                            PhoneNumber = "0762004256",
                            Password = passwordHasher.HashPassword(null, "Admin123"),
                            Role = "Admin",
                            MustChangePassword = true 
                        };

                        context.Users.Add(adminUser);
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // ✅ Add Authentication & Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Default route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Guest}/{action=Home}/{id?}");

            app.Run();
        }
    }
}
