using System.Collections.Generic;
using System.Linq;
using MediCare.Data;
using MediCare.Models;

namespace MediCare.Services
{
    public class DoctorService
    {
        private readonly ApplicationDbContext _context;

        public DoctorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddDoctor(Doctor doctor)
        {
            _context.Doctors.Add(doctor);
            _context.SaveChanges();
        }

        public List<Doctor> GetAllDoctors()
        {
            return _context.Doctors.ToList();
        }

        public Doctor? GetDoctorById(int id)
        {
            return _context.Doctors.FirstOrDefault(d => d.Id == id);
        }

        public Doctor? GetDoctorByEmail(string email)
        {
            return _context.Doctors.FirstOrDefault(d => d.Email.ToLower() == (email ?? "").ToLower());
        }

        public void UpdateDoctor(Doctor doctor)
        {
            _context.Doctors.Update(doctor);
            _context.SaveChanges();
        }

        public void DeleteDoctor(int id)
        {
            var doctor = GetDoctorById(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                _context.SaveChanges();
            }
        }
    }
}
