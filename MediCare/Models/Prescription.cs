using System;

namespace MediCare.Models
{
    public class Prescription
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; }
        
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        public DateTime DateIssued { get; set; }
        public string Diagnosis { get; set; }
        public string MedicineNotes { get; set; }
        public string AdditionalNotes { get; set; }
    }
}
