using MediCare.Data;
using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services
{
    public class LoginSessionService
    {
        private readonly ApplicationDbContext _context;

        public LoginSessionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public LoginSessionRecord StartSession(string email, string name, string role)
        {
            var record = new LoginSessionRecord
            {
                UserEmail = (email ?? string.Empty).Trim().ToLower(),
                UserName = name ?? string.Empty,
                Role = role ?? string.Empty,
                LoginAt = DateTime.UtcNow,
                LogoutAt = null,
                IsActive = true
            };

            _context.LoginSessionRecords.Add(record);
            _context.SaveChanges();
            return record;
        }

        public void EndSession(int sessionId)
        {
            var record = _context.LoginSessionRecords.FirstOrDefault(x => x.Id == sessionId);
            if (record == null) return;

            record.LogoutAt = DateTime.UtcNow;
            record.IsActive = false;
            _context.SaveChanges();
        }

        public LoginSessionRecord? GetActiveSession(string email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLower();
            return _context.LoginSessionRecords
                .OrderByDescending(x => x.LoginAt)
                .FirstOrDefault(x => x.UserEmail.ToLower() == normalized && x.IsActive);
        }

        public LoginSessionRecord? GetLatestSession(string email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLower();
            return _context.LoginSessionRecords
                .OrderByDescending(x => x.LoginAt)
                .FirstOrDefault(x => x.UserEmail.ToLower() == normalized);
        }
    }
}
