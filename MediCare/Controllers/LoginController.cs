using MediCare.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class LoginController : Controller
    {
        private readonly EmailService _emailService;
        private readonly UserService _userService;

        public LoginController(EmailService emailService, UserService userService)
        {
            _emailService = emailService;
            _userService = userService;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string Email, string Password)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Please enter both email and password.";
                return View();
            }

            // Default Admin (Bypass approval check)
            if (Email.ToLower() == "agherabansi2@gmail.com" && Password == "201040")
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                return RedirectToAction("Dashboard", "Admin");
            }

            var user = _userService.GetUserByEmail(Email);
            if (user != null && user.Password == Password)
            {
                if (user.Status != "Approved")
                {
                    ViewBag.Error = $"Your account is {user.Status}. Please wait for admin approval.";
                    return View();
                }

                HttpContext.Session.SetString("UserRole", user.Role);
                return RedirectToAction("Dashboard", user.Role);
            }

            ViewBag.Error = "Invalid Login Credentials!";
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                ViewBag.Error = "Please enter your email.";
                return View();
            }

            Random random = new Random();
            string otp = random.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("ResetEmail", Email);

            string subject = "MediCare Password Reset OTP";

            string body = "<h2>Your OTP is: " + otp + "</h2><p>This OTP is valid for 5 minutes.</p>";

            _emailService.SendEmail(Email, subject, body);

            return RedirectToAction("VerifyOTP");
        }

        public IActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyOTP(string UserOTP)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");

            if (UserOTP == sessionOtp)
            {
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Invalid OTP!";
            return View();
        }

        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match!";
                return View();
            }

            return RedirectToAction("Login");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string FirstName, string LastName, string Email, string phone, string Password, string Role)
        {
            var newUser = new MediCare.Models.AppUser
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Phone = phone,
                Password = Password,
                Role = Role
            };

            _userService.AddUser(newUser);

            // After registration, redirect to login page as requested
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}