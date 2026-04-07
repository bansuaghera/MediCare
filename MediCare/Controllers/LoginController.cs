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

        // ===================== LOGIN =====================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string? email, string? password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter both email and password.";
                return View();
            }

            email = email.ToLower();

            // Hardcoded Admin Login
            if (email == "agherabansi2@gmail.com" && password == "201040")
            {
                SetSession("Admin", "Super Admin", email);
                return RedirectToAction("Dashboard", "Admin");
            }

            var user = _userService.GetUserByEmail(email);

            if (user == null || user.Password != password)
            {
                ViewBag.Error = "Invalid Login Credentials!";
                return View();
            }

            if (user.Status != "Approved")
            {
                ViewBag.Error = $"Your account is {user.Status}. Please wait for admin approval.";
                return View();
            }

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            SetSession(user.Role, $"{user.FirstName} {user.LastName}", user.Email);

            string controller = user.Role switch
            {
                "Patient" => "User", // Patient role maps to UserController
                "Admin" => "Admin",
                "Doctor" => "Doctor",
                "Staff" => "Staff",
                _ => user.Role
            };

            return RedirectToAction("Dashboard", controller);
        }

        // ===================== LOGOUT =====================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Login");
        }

        // ===================== FORGOT PASSWORD =====================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email.";
                return View();
            }

            string otp = GenerateOtp();

            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("ResetEmail", email);

            SendOtpEmail(email, otp);

            return RedirectToAction("VerifyOTP");
        }

        // ===================== VERIFY OTP =====================

        [HttpGet]
        public IActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyOTP(string? userOtp)
        {
            var sessionOtp = HttpContext.Session.GetString("OTP");

            if (userOtp == sessionOtp)
            {
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Invalid OTP!";
            return View();
        }

        // ===================== RESET PASSWORD =====================

        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match!";
                return View();
            }

            var email = HttpContext.Session.GetString("ResetEmail");

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            _userService.UpdateUserPassword(email, newPassword);

            // Clear session
            HttpContext.Session.Remove("ResetEmail");
            HttpContext.Session.Remove("OTP");

            TempData["Success"] = "Password reset successful! Please login.";
            return RedirectToAction("Login");
        }

        // ===================== REGISTER =====================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string? firstName, string? lastName, string? email,
                                       string? phone, string? password, string? role)
        {
            var user = new MediCare.Models.AppUser
            {
                FirstName = firstName ?? "",
                LastName = lastName ?? "",
                Email = email ?? "",
                Phone = phone ?? "",
                Password = password ?? "",
                Role = role ?? "Patient",
                Status = "Approved" // Set to Approved for instant testing
            };

            _userService.AddUser(user);

            return RedirectToAction("Login");
        }

        // ===================== HELPERS =====================

        private void SetSession(string role, string name, string email)
        {
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("UserName", name);
            HttpContext.Session.SetString("UserEmail", email);
        }

        private string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private void SendOtpEmail(string email, string otp)
        {
            string subject = "MediCare Password Reset OTP";

            string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family:Segoe UI;background:#f4f6f9;padding:30px;'>
<div style='max-width:600px;margin:auto;background:white;border-radius:10px;padding:30px;text-align:center;'>
<h2 style='color:#16a34a;'>MediCare+</h2>
<h3>Password Reset Verification</h3>
<p>Use the OTP below to reset your password</p>
<div style='font-size:28px;font-weight:bold;background:#f1f5f9;padding:15px;border-radius:8px;display:inline-block;letter-spacing:5px;'>
{otp}
</div>
<p style='margin-top:20px;'>This OTP is valid for 5 minutes.</p>
</div>
</body>
</html>";

            _emailService.SendEmail(email, subject, body);
        }
    }
}