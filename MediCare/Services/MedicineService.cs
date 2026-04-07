using System.Collections.Generic;
using System.Linq;
using MediCare.Data;
using MediCare.Models;

namespace MediCare.Services
{
    public class MedicineService
    {
        private readonly ApplicationDbContext _context;

        public MedicineService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddMedicine(Medicine medicine)
        {
            _context.Medicines.Add(medicine);
            _context.SaveChanges();
        }

        public List<Medicine> GetAllMedicines()
        {
            return _context.Medicines.ToList();
        }

        public Medicine? GetMedicineById(int id)
        {
            return _context.Medicines.FirstOrDefault(m => m.Id == id);
        }

        public void UpdateMedicine(Medicine medicine)
        {
            _context.Medicines.Update(medicine);
            _context.SaveChanges();
        }

        public void DeleteMedicine(int id)
        {
            var medicine = GetMedicineById(id);
            if (medicine != null)
            {
                _context.Medicines.Remove(medicine);
                _context.SaveChanges();
            }
        }
    }
}
