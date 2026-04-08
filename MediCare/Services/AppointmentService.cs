using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MediCare.Data;
using MediCare.Models;

namespace MediCare.Services
{
    public class AppointmentService
    {
        private readonly ApplicationDbContext _context;

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddAppointment(Appointment appointment)
        {
            // set queue priority
            var maxSort = _context.Appointments.Any() ? _context.Appointments.Max(a => a.SortOrder) : 0;
            appointment.SortOrder = maxSort + 1;

            // assign token if missing
            if (string.IsNullOrWhiteSpace(appointment.TokenNumber))
            {
                appointment.TokenNumber = GenerateTokenNumber(appointment.IsEmergency, appointment.SortOrder);
            }
            _context.Appointments.Add(appointment);
            _context.SaveChanges();
        }

        public List<Appointment> GetAllAppointments()
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.IsEmergency)
                .ThenBy(a => a.SortOrder)
                .ThenBy(a => a.AppointmentDate)
                .ToList();
        }

        public Appointment? GetAppointmentById(int id)
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefault(a => a.Id == id);
        }

        public void UpdateAppointment(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
            _context.SaveChanges();
        }

        public void UpdateSortOrder(IList<int> orderedIds)
        {
            // lower index => higher priority
            var map = _context.Appointments.Where(a => orderedIds.Contains(a.Id)).ToList();
            for (int i = 0; i < orderedIds.Count; i++)
            {
                var appt = map.FirstOrDefault(a => a.Id == orderedIds[i]);
                if (appt != null)
                {
                    appt.SortOrder = i + 1;
                    appt.TokenNumber = GenerateTokenNumber(appt.IsEmergency, i + 1);
                }
            }
            _context.SaveChanges();
        }

        public void NormalizeTokens(IEnumerable<Appointment> orderedAppointments)
        {
            int idx = 1;
            foreach (var appt in orderedAppointments)
            {
                appt.SortOrder = idx;
                appt.TokenNumber = GenerateTokenNumber(appt.IsEmergency, idx);
                idx++;
            }
            _context.SaveChanges();
        }

        private string GenerateTokenNumber(bool isEmergency, int sequence)
        {
            var prefix = isEmergency ? "EMG-" : "TN-";
            return $"{prefix}{sequence:D3}";
        }

        public void DeleteAppointment(int id)
        {
            var appointment = GetAppointmentById(id);
            if (appointment != null)
            {
                var relatedPrescriptions = _context.Prescriptions
                    .Where(p => p.AppointmentId == id)
                    .ToList();

                if (relatedPrescriptions.Count > 0)
                {
                    _context.Prescriptions.RemoveRange(relatedPrescriptions);
                }

                _context.Appointments.Remove(appointment);
                _context.SaveChanges();
            }
        }
    }
}
