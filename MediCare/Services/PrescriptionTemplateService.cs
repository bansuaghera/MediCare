using MediCare.Data;
using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services
{
    public class PrescriptionTemplateService
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionTemplateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PrescriptionTemplate>> GetAllTemplatesAsync()
        {
            return await _context.PrescriptionTemplates.OrderByDescending(t => t.Title).ToListAsync();
        }

        public async Task AddTemplateAsync(PrescriptionTemplate template)
        {
            _context.PrescriptionTemplates.Add(template);
            await _context.SaveChangesAsync();
        }

        public async Task<PrescriptionTemplate?> GetTemplateByIdAsync(int id)
        {
            return await _context.PrescriptionTemplates.FindAsync(id);
        }

        public async Task DeleteTemplateAsync(int id)
        {
            var template = await _context.PrescriptionTemplates.FindAsync(id);
            if (template != null)
            {
                _context.PrescriptionTemplates.Remove(template);
                await _context.SaveChangesAsync();
            }
        }
    }
}
