using System;
using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class LoginSessionRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public DateTime LoginAt { get; set; } = DateTime.UtcNow;

        public DateTime? LogoutAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
