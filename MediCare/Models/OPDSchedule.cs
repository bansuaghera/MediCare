using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class OPDSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [Required]
        public string DayOfWeek { get; set; } // Monday, Tuesday, etc.

        [Required]
        public string StartTime { get; set; }

        [Required]
        public string EndTime { get; set; }

        public int MaxPatients { get; set; }
        public string RoomNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
