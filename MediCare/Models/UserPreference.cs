using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class UserPreference
    {
        public int Id { get; set; }

        [Required]
        public string UserEmail { get; set; } = string.Empty;

        public bool PushNotificationsEnabled { get; set; } = true;

        public bool TwoFactorEnabled { get; set; } = false;
    }
}
