using System;
using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        public string? TimeSlot { get; set; }
        public string? Status { get; set; } // Scheduled, Completed, Cancelled
        public string? Notes { get; set; }
        public string? Subject { get; set; }
        public string? TokenNumber { get; set; }
        public bool IsEmergency { get; set; }
        public int SortOrder { get; set; } // lower means higher priority in queue
    }
}
