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
            return await _context.PrescriptionTemplates
                .Where(t => t.EntryType == "Template")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PrescriptionTemplate>> GetAllBranchesAsync()
        {
            return await _context.PrescriptionTemplates
                .Where(t => t.EntryType == "Branch")
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task AddTemplateAsync(PrescriptionTemplate template)
        {
            template.EntryType = "Template";
            _context.PrescriptionTemplates.Add(template);
            await _context.SaveChangesAsync();
        }

        public async Task AddBranchAsync(PrescriptionTemplate branch)
        {
            branch.EntryType = "Branch";
            branch.Diagnosis = branch.Diagnosis ?? string.Empty;
            branch.MedicineNotes = branch.MedicineNotes ?? string.Empty;
            branch.AdditionalNotes = branch.AdditionalNotes ?? string.Empty;
            _context.PrescriptionTemplates.Add(branch);
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
