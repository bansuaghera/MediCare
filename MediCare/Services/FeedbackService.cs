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

        public async Task<Feedback?> GetFeedbackByIdAsync(int id)
        {
            return await _context.Feedbacks.FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<List<Feedback>> GetFeedbacksForPatientAsync(int patientId)
        {
            return await _context.Feedbacks
                .Where(f => f.PatientId == patientId)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<Feedback>> GetFeedbacksByNameAsync(string name)
        {
            var normalized = (name ?? string.Empty).Trim().ToLower();
            return await _context.Feedbacks
                .Where(f => f.PatientName.ToLower() == normalized)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();
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

        public async Task DeleteFeedbacksAsync(IEnumerable<int> ids)
        {
            var feedbacks = await _context.Feedbacks.Where(f => ids.Contains(f.Id)).ToListAsync();
            if (feedbacks.Count == 0) return;
            _context.Feedbacks.RemoveRange(feedbacks);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllForPatientAsync(int patientId)
        {
            var feedbacks = await _context.Feedbacks.Where(f => f.PatientId == patientId).ToListAsync();
            if (feedbacks.Count == 0) return;
            _context.Feedbacks.RemoveRange(feedbacks);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllForNameAsync(string name)
        {
            var normalized = (name ?? string.Empty).Trim().ToLower();
            var feedbacks = await _context.Feedbacks.Where(f => f.PatientName.ToLower() == normalized).ToListAsync();
            if (feedbacks.Count == 0) return;
            _context.Feedbacks.RemoveRange(feedbacks);
            await _context.SaveChangesAsync();
        }
    }
}
