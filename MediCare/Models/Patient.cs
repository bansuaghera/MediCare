using System;
using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public DateTime DateOfBirth { get; set; }
        [Required]
        public string Gender { get; set; } = "Not Specified";

        [Required]
        public string BloodGroup { get; set; } = "Not Specified";

        [Required]
        public string Phone { get; set; }

        public string Email { get; set; }

        [Required]
        public string Address { get; set; } = "Not Specified";

        [Required]
        public string MedicalHistory { get; set; } = "None";
    }
}
