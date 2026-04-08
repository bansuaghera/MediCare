using MediCare.Data;
using MediCare.Models;

namespace MediCare.Services
{
    public class UserPreferenceService
    {
        private readonly ApplicationDbContext _context;

        public UserPreferenceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public UserPreference GetOrCreate(string email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLower();
            var pref = _context.UserPreferences.FirstOrDefault(x => x.UserEmail.ToLower() == normalized);
            if (pref != null) return pref;

            pref = new UserPreference
            {
                UserEmail = normalized,
                PushNotificationsEnabled = true,
                TwoFactorEnabled = false
            };
            _context.UserPreferences.Add(pref);
            _context.SaveChanges();
            return pref;
        }

        public bool IsPushEnabled(string email)
        {
            return GetOrCreate(email).PushNotificationsEnabled;
        }

        public bool IsTwoFactorEnabled(string email)
        {
            return GetOrCreate(email).TwoFactorEnabled;
        }

        public void SetPushEnabled(string email, bool enabled)
        {
            var pref = GetOrCreate(email);
            pref.PushNotificationsEnabled = enabled;
            _context.SaveChanges();
        }

        public void SetTwoFactorEnabled(string email, bool enabled)
        {
            var pref = GetOrCreate(email);
            pref.TwoFactorEnabled = enabled;
            _context.SaveChanges();
        }
    }
}
