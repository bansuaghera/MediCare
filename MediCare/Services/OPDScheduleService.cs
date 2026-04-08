using MediCare.Data;
using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services
{
    public class OPDScheduleService
    {
        private readonly ApplicationDbContext _context;

        public OPDScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<OPDSchedule>> GetAllSchedulesAsync()
        {
            return await _context.OPDSchedules.Include(s => s.Doctor).ToListAsync();
        }

        public async Task AddScheduleAsync(OPDSchedule schedule)
        {
            _context.OPDSchedules.Add(schedule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateScheduleAsync(OPDSchedule schedule)
        {
            _context.OPDSchedules.Update(schedule);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteScheduleAsync(int id)
        {
            var schedule = await _context.OPDSchedules.FindAsync(id);
            if (schedule != null)
            {
                _context.OPDSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
        }
    }
}
