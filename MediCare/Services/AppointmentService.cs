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
            _context.Appointments.Add(appointment);
            _context.SaveChanges();
        }

        public List<Appointment> GetAllAppointments()
        {
            return _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
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

        public void DeleteAppointment(int id)
        {
            var appointment = GetAppointmentById(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                _context.SaveChanges();
            }
        }
    }
}
