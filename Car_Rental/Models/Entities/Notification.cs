using Car_Rental.Enum;
using System.ComponentModel.DataAnnotations;

namespace Car_Rental.Models.Entities
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = null!;
    }
}
