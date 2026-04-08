using System;
using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class ClinicBranch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Location { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
