using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Specialty { get; set; }

        [Required]
        public string LicenseNumber { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Email { get; set; }

        public decimal ConsultationFee { get; set; }
        public int ExperienceYears { get; set; }
    }
}
