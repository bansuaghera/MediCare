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

            // Generate 6 digit OTP
            Random random = new Random();
            string otp = random.Next(100000, 999999).ToString();

            // Store OTP in session
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("ResetEmail", Email);

            string subject = "MediCare Password Reset OTP";
            string body = @"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0; padding:0; background-color:#f4f6f9; font-family:Segoe UI, Arial, sans-serif;'>

<table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
<tr>
<td align='center'>

<table width='600' cellpadding='0' cellspacing='0' 
style='background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 10px 30px rgba(0,0,0,0.1);'>

<!-- Header -->
<tr>
<td style='background: linear-gradient(135deg,#16a34a,#14b8a6); padding:30px; text-align:center; color:white;'>
<h2 style='margin:0;'>MediCare+</h2>
<p style='margin:5px 0 0 0;'>Smart Hospital OPD & Patient Care</p>
</td>
</tr>

<!-- Body -->
<tr>
<td style='padding:40px; text-align:center;'>

<h3 style='margin-top:0;'>Password Reset Verification</h3>

<p style='color:#555; font-size:15px;'>
We received a request to reset your password.
Use the OTP below to continue.
</p>

<div style='margin:30px 0; font-size:28px; font-weight:bold; letter-spacing:6px; 
background:#f1f5f9; padding:15px 25px; display:inline-block; border-radius:8px;'>
" + otp + @"
</div>

<p style='color:#777; font-size:14px;'>
This OTP is valid for <strong>5 minutes</strong>.
Do not share this code with anyone.
</p>

<hr style='margin:30px 0; border:none; border-top:1px solid #eee;'>

<p style='font-size:13px; color:#999;'>
If you didn't request this, please ignore this email.
</p>

</td>
</tr>

<!-- Footer -->
<tr>
<td style='background:#f9fafb; padding:20px; text-align:center; font-size:13px; color:#777;'>
Need help? Contact us at <br>
<strong style='color:#16a34a;'>support@medicare.com</strong>
</td>
</tr>

</table>

</td>
</tr>
</table>

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
    }
}