using System.ComponentModel.DataAnnotations;
namespace Car_Rental.Models.Entities
{
    public class Guest
    {
        [Key]
        public int Id { get; set; }


        public Booking BookingDetails { get; set; }


        public string FullName { get; set; }


        public string Email { get; set; }


        public string PhoneNumber { get; set; }
    }
}
