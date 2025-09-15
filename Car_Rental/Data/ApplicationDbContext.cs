using Car_Rental.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Car_Rental.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<DamageReport> DamageReports { get; set; }
        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    // User -> Bookings
        //    modelBuilder.Entity<User>()
        //        .HasMany(u => u.Bookings)
        //        .WithOne(b => b.User)
        //        .HasForeignKey(b => b.UserID)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    // User -> Reviews
        //    modelBuilder.Entity<User>()
        //        .HasMany(u => u.Reviews)
        //        .WithOne(r => r.User)
        //        .HasForeignKey(r => r.UserId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // User -> Notifications
        //    modelBuilder.Entity<User>()
        //        .HasMany(u => u.Notifications)
        //        .WithOne(n => n.User)
        //        .HasForeignKey(n => n.UserId)
        //        .OnDelete(DeleteBehavior.SetNull);

        //    // Car -> Bookings
        //    modelBuilder.Entity<Car>()
        //        .HasMany(c => c.Bookings)
        //        .WithOne(b => b.Car)
        //        .HasForeignKey(b => b.CarID)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    // Car -> Reviews
        //    modelBuilder.Entity<Car>()
        //        .HasMany(c => c.Reviews)
        //        .WithOne(r => r.Car)
        //        .HasForeignKey(r => r.CarId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Booking -> Payments
        //    modelBuilder.Entity<Booking>()
        //        .HasMany(b => b.Payments)
        //        .WithOne(p => p.Booking)
        //        .HasForeignKey(p => p.BookingId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Booking -> Invoices
        //    modelBuilder.Entity<Booking>()
        //        .HasMany(b => b.Invoices)
        //        .WithOne(i => i.Booking)
        //        .HasForeignKey(i => i.BookingId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Booking -> DamageReports
        //    modelBuilder.Entity<Booking>()
        //        .HasMany(b => b.DamageReports)
        //        .WithOne(d => d.Booking)
        //        .HasForeignKey(d => d.BookingId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Driver -> Bookings
        //    modelBuilder.Entity<Driver>()
        //        .HasMany(d => d.Bookings)
        //        .WithOne(b => b.Driver)
        //        .HasForeignKey(b => b.DriverID)
        //        .OnDelete(DeleteBehavior.SetNull);

        //    // DiscountCode -> Bookings
        //    modelBuilder.Entity<DiscountCode>()
        //        .HasMany(d => d.Bookings)
        //        .WithOne(b => b.DiscountCode)
        //        .HasForeignKey(b => b.DiscountCodeId)
        //        .OnDelete(DeleteBehavior.SetNull);

        //    // Insurance -> Bookings
        //    modelBuilder.Entity<Insurance>()
        //        .HasMany(i => i.Bookings)
        //        .WithOne(b => b.Insurance)
        //        .HasForeignKey(b => b.InsuranceId)
        //        .OnDelete(DeleteBehavior.SetNull);

        //    // Insurance -> DamageReports
        //    modelBuilder.Entity<Insurance>()
        //        .HasMany(i => i.DamageReports)
        //        .WithOne(d => d.Insurance)
        //        .HasForeignKey(d => d.InsuranceID)
        //        .OnDelete(DeleteBehavior.SetNull);
    }
}

