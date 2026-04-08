using MediCare.Models;

namespace MediCare.Utilities
{
    public static class AnalyticsHelper
    {
        public static (List<string> Labels, List<int> Counts) BuildDailyAppointmentTrend(IEnumerable<Appointment> appointments, int days = 7)
        {
            var end = DateTime.Today;
            var dates = Enumerable.Range(0, days)
                .Select(i => end.AddDays(-(days - 1 - i)))
                .ToList();

            var labels = dates.Select(d => d.ToString("ddd")).ToList();
            var counts = dates.Select(d => appointments.Count(a => a.AppointmentDate.Date == d)).ToList();
            return (labels, counts);
        }

        public static (List<string> Labels, List<int> Counts) BuildDailyUniquePatientTrend(IEnumerable<Appointment> appointments, int days = 7)
        {
            var end = DateTime.Today;
            var dates = Enumerable.Range(0, days)
                .Select(i => end.AddDays(-(days - 1 - i)))
                .ToList();

            var labels = dates.Select(d => d.ToString("ddd")).ToList();
            var counts = dates.Select(d => appointments.Where(a => a.AppointmentDate.Date == d).Select(a => a.PatientId).Distinct().Count()).ToList();
            return (labels, counts);
        }

        public static (List<string> Labels, List<int> Counts) BuildStatusCounts(IEnumerable<Appointment> appointments)
        {
            var labels = new List<string> { "Waiting", "In Progress", "Completed", "Emergency" };
            var counts = new List<int>
            {
                appointments.Count(a => a.Status == "Waiting" || a.Status == "Scheduled"),
                appointments.Count(a => a.Status == "In Progress"),
                appointments.Count(a => a.Status == "Completed"),
                appointments.Count(a => a.IsEmergency)
            };
            return (labels, counts);
        }

        public static List<int> BuildHourlyTrend(IEnumerable<Appointment> appointments, int hoursBack = 12)
        {
            var now = DateTime.Today.AddHours(DateTime.Now.Hour);
            return Enumerable.Range(0, hoursBack)
                .Select(i =>
                {
                    var target = now.AddHours(-(hoursBack - 1 - i));
                    return appointments.Count(a => a.AppointmentDate.Date == target.Date && int.TryParse(a.TimeSlot?.Split(':').FirstOrDefault(), out var hr) && hr == target.Hour);
                })
                .ToList();
        }
    }
}
