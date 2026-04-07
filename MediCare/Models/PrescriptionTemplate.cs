using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class PrescriptionTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Diagnosis { get; set; }

        [Required]
        public string MedicineNotes { get; set; }

        public string? AdditionalNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
