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

        // LOGIN PAGE
        public IActionResult Login()
        {
            return View();
        }

        // LOGIN POST
        [HttpPost]
        public IActionResult Login(string Email, string Password)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Please enter both email and password.";
                return View();
            }

            Email = Email.ToLower();

            if (Email == "admin@medicare" && Password == "123")
            {
                HttpContext.Session.SetString("Role", "Admin");
                return RedirectToAction("Dashboard", "Admin");
            }
            else if (Email == "doctor@medicare" && Password == "123")
            {
                HttpContext.Session.SetString("Role", "Doctor");
                return RedirectToAction("Dashboard", "Doctor");
            }
            else if (Email == "staff@medicare" && Password == "123")
            {
                HttpContext.Session.SetString("Role", "Staff");
                return RedirectToAction("Dashboard", "Staff");
            }
            else if (Email == "user@medicare" && Password == "123")
            {
                HttpContext.Session.SetString("Role", "User");
                return RedirectToAction("Dashboard", "User");
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // LOGOUT
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // FORGOT PASSWORD PAGE
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // SEND OTP
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

            string body = @"
<!DOCTYPE html>
<html>
<body style='font-family:Segoe UI;background:#f4f6f9;padding:30px;'>

<div style='max-width:600px;margin:auto;background:white;border-radius:10px;padding:30px;text-align:center;'>

<h2 style='color:#16a34a;'>MediCare+</h2>
<h3>Password Reset Verification</h3>

<p>Use the OTP below to reset your password</p>

<div style='font-size:28px;font-weight:bold;background:#f1f5f9;padding:15px;border-radius:8px;display:inline-block;letter-spacing:5px;'>
" + otp + @"
</div>

<p style='margin-top:20px;'>This OTP is valid for 5 minutes.</p>

</div>

</body>
</html>
";

            _emailService.SendEmail(Email, subject, body);

            return RedirectToAction("VerifyOTP");
        }

        // OTP PAGE
        public IActionResult VerifyOTP()
        {
            return View();
        }

        // VERIFY OTP
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

        // RESET PASSWORD PAGE
        public IActionResult ResetPassword()
        {
            return View();
        }

        // RESET PASSWORD POST
        [HttpPost]
        public IActionResult ResetPassword(string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match!";
                return View();
            }

            // TODO: Update password in database

            return RedirectToAction("Login");
        }

        // REGISTER PAGE
        public IActionResult Register()
        {
            return View();
        }
    }
}