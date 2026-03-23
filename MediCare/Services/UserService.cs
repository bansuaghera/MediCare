using System.Collections.Generic;
using System.Linq;
using MediCare.Models;

namespace MediCare.Services
{
    public class UserService
    {
        private static List<AppUser> _users = new List<AppUser>()
        {
            new AppUser { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "1234567890", Role = "Admin", Status = "Approved", Password = "123" },
            new AppUser { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "admin@medicare", Phone = "0987654321", Role = "Admin", Status = "Approved", Password = "123" },
            new AppUser { Id = 3, FirstName = "Alice", LastName = "Johnson", Email = "doctor@medicare", Phone = "5551234567", Role = "Doctor", Status = "Approved", Password = "123" },
            new AppUser { Id = 4, FirstName = "Bob", LastName = "Williams", Email = "staff@medicare", Phone = "5559876543", Role = "Staff", Status = "Approved", Password = "123" },
            new AppUser { Id = 5, FirstName = "Charlie", LastName = "Brown", Email = "user@medicare", Phone = "5555555555", Role = "User", Status = "Pending", Password = "123" }
        };
        private static int _nextId = 6;

        public void AddUser(AppUser user)
        {
            user.Id = _nextId++;
            user.Status = "Pending";
            _users.Add(user);
        }

        public List<AppUser> GetAllUsers()
        {
            return _users;
        }

        public List<AppUser> GetPendingUsers()
        {
            return _users.Where(u => u.Status == "Pending").ToList();
        }

        public AppUser GetUserByEmail(string email)
        {
            return _users.FirstOrDefault(u => u.Email.Equals(email, System.StringComparison.OrdinalIgnoreCase));
        }

        public AppUser GetUserById(int id)
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }

        public void UpdateUserStatus(int id, string status)
        {
            var user = GetUserById(id);
            if (user != null)
            {
                user.Status = status;
            }
        }

        public void UpdateUserRole(int id, string role)
        {
            var user = GetUserById(id);
            if (user != null)
            {
                user.Role = role;
            }
        }

        public void RemoveUser(int id)
        {
            var user = GetUserById(id);
            if (user != null)
            {
                _users.Remove(user);
            }
        }
    }
}
