using System;
using System.Collections.Generic;
using MediCare.Models;

namespace MediCare.Models.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public int TodayAppointmentsCount { get; set; }
        public int WaitingPatientsCount { get; set; }
        public int CompletedAppointmentsCount { get; set; }
        public int TomorrowAppointmentsCount { get; set; }
        public List<Appointment> TodayAppointments { get; set; } = new List<Appointment>();
        public int TotalPatientsCount { get; set; }
        public int PrescriptionsIssuedCount { get; set; }
        public double TodayProgressPercentage { get; set; }
    }
}
