using MediCare.Data;
using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services
{
    public class FeedbackService
    {
        private readonly ApplicationDbContext _context;

        public FeedbackService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Feedback>> GetAllFeedbacksAsync()
        {
            return await _context.Feedbacks.Include(f => f.Patient).OrderByDescending(f => f.SubmittedAt).ToListAsync();
        }

        public async Task AddFeedbackAsync(Feedback feedback)
        {
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteFeedbackAsync(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
            }
        }
    }
}
