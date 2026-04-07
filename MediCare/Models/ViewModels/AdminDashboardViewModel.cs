using System;
using System.Collections.Generic;

namespace MediCare.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int AppointmentsToday { get; set; }
        public int ActiveDoctors { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Appointment>? RecentAppointments { get; set; }
        
        // For Charts
        public List<int> WeeklyPatients { get; set; } = new List<int>();
        public List<int> WeeklyAppointments { get; set; } = new List<int>();
        public List<string> ChartLabels { get; set; } = new List<string>();
    }
}
