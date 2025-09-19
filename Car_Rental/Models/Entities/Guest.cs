using System.ComponentModel.DataAnnotations;
namespace Car_Rental.Models.Entities
{
    public class Guest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Booking BookingDetails { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }
    }
}
