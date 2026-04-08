using System.Collections.Generic;
using System.Linq;
using MediCare.Data;
using MediCare.Models;

namespace MediCare.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddUser(AppUser user)
        {
            // Auto-approve patients (Users), but keep Staff and Doctors pending admin approval
            user.Status = (user.Role == "User" || user.Role == "Patient") ? "Approved" : "Pending";
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public List<AppUser> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        public List<AppUser> GetPendingUsers()
        {
            return _context.Users.Where(u => u.Status == "Pending").ToList();
        }

        public AppUser? GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
        }

        public List<AppUser> GetStaffUsers()
        {
            return _context.Users.Where(u => u.Role == "Staff").ToList();
        }

        public AppUser? GetUserById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }

        public void UpdateUserStatus(int id, string status)
        {
            var user = GetUserById(id);
            if (user != null)
            {
                user.Status = status;
                _context.SaveChanges();
            }
        }

        public void UpdateUserRole(int id, string role)
        {
            var user = GetUserById(id);
            if (user != null)
            {
                user.Role = role;
                _context.SaveChanges();
            }
        }

        public void RemoveUser(int id)
        {
            var user = GetUserById(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }

        public void UpdateUserPassword(string email, string newPassword)
        {
            var user = GetUserByEmail(email);
            if (user != null)
            {
                user.Password = newPassword;
                _context.SaveChanges();
            }
        }
    }
}
