using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        public int? PatientId { get; set; }
        public Patient? Patient { get; set; }

        public string PatientName { get; set; } // For anonymous or non-registered feedback

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        public string Comment { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public bool IsVisible { get; set; } = true;
    }
}
