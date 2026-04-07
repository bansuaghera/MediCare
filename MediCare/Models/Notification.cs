using System;
using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserEmail { get; set; } = string.Empty; // target user

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = "info"; // info, success, warning, error

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
