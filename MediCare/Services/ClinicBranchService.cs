using MediCare.Data;
using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services
{
    public class ClinicBranchService
    {
        private readonly ApplicationDbContext _context;

        public ClinicBranchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClinicBranch>> GetAllBranchesAsync()
        {
            return await _context.ClinicBranches.OrderBy(b => b.Name).ToListAsync();
        }

        public async Task<ClinicBranch?> GetBranchByIdAsync(int id)
        {
            return await _context.ClinicBranches.FindAsync(id);
        }

        public async Task AddBranchAsync(ClinicBranch branch)
        {
            _context.ClinicBranches.Add(branch);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBranchAsync(int id)
        {
            var branch = await _context.ClinicBranches.FindAsync(id);
            if (branch != null)
            {
                _context.ClinicBranches.Remove(branch);
                await _context.SaveChangesAsync();
            }
        }
    }
}
