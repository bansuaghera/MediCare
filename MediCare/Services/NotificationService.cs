using System.Collections.Generic;
using System.Linq;
using MediCare.Data;
using MediCare.Models;

namespace MediCare.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddNotification(Notification notification)
        {
            _context.Notifications.Add(notification);
            _context.SaveChanges();
        }

        public List<Notification> GetNotificationsForUser(string email, int take = 50)
        {
            return _context.Notifications
                .Where(n => n.UserEmail.ToLower() == email.ToLower())
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToList();
        }

        public void DeleteNotification(int id, string email)
        {
            var notif = _context.Notifications.FirstOrDefault(n => n.Id == id && n.UserEmail.ToLower() == email.ToLower());
            if (notif != null)
            {
                _context.Notifications.Remove(notif);
                _context.SaveChanges();
            }
        }

        public void DeleteAllForUser(string email)
        {
            var notifs = _context.Notifications.Where(n => n.UserEmail.ToLower() == email.ToLower());
            _context.Notifications.RemoveRange(notifs);
            _context.SaveChanges();
        }

        public void MarkAllRead(string email)
        {
            var notifs = _context.Notifications.Where(n => n.UserEmail.ToLower() == email.ToLower() && !n.IsRead);
            foreach (var n in notifs)
            {
                n.IsRead = true;
            }
            _context.SaveChanges();
        }
    }
}
