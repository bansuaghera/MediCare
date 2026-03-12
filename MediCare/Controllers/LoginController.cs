using MediCare.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class LoginController : Controller
    {
        private readonly EmailService _emailService;

        public LoginController(EmailService emailService)
        {
            _emailService = emailService;
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

            if (Email.ToLower() == "doctor@medicare" && Password == "123")
            {
                return RedirectToAction("Dashboard", "Doctor");
            }
            else if (Email.ToLower() == "user@medicare" && Password == "123")
            {
                return RedirectToAction("Dashboard", "User");
            }
            else if (Email.ToLower() == "admin@medicare" && Password == "123")
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else if (Email.ToLower() == "staff@medicare" && Password == "123")
            {
                return RedirectToAction("Dashboard", "Staff");
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

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}