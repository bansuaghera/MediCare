using MediCare.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace MediCare.Controllers
{
    public class LoginController : Controller
    {
        private readonly EmailService _emailService;
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;
        private readonly UserPreferenceService _userPreferenceService;
        private readonly LoginSessionService _loginSessionService;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public LoginController(EmailService emailService, UserService userService, NotificationService notificationService, UserPreferenceService userPreferenceService, LoginSessionService loginSessionService, IAuthenticationSchemeProvider schemeProvider)
        {
            _emailService = emailService;
            _userService = userService;
            _notificationService = notificationService;
            _userPreferenceService = userPreferenceService;
            _loginSessionService = loginSessionService;
            _schemeProvider = schemeProvider;
        }

        // ===================== LOGIN =====================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                TempData["Success"] = "Please choose Google or GitHub.";
                return RedirectToAction("Login");
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Login", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            if (!string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(provider, "GitHub", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Success"] = "Unsupported external login provider.";
                return RedirectToAction("Login");
            }

            var configured = _schemeProvider.GetAllSchemesAsync().GetAwaiter().GetResult()
                .Any(s => string.Equals(s.Name, provider, StringComparison.OrdinalIgnoreCase));
            if (!configured)
            {
                TempData["Success"] = $"{provider} sign-in is not configured yet. Add Client ID and Secret in appsettings.";
                return RedirectToAction("Login");
            }

            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var authResult = await HttpContext.AuthenticateAsync("External");
            if (!authResult.Succeeded || authResult.Principal == null)
            {
                TempData["Success"] = "External login failed. Please try again.";
                return RedirectToAction("Login");
            }

            var principal = authResult.Principal;
            var email = principal.FindFirstValue(ClaimTypes.Email)
                        ?? principal.FindFirstValue("email")
                        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync("External");
                TempData["Success"] = "We could not read your email from Google/GitHub. Please try another sign-in method.";
                return RedirectToAction("Login");
            }

            email = email.Trim().ToLower();
            var firstName = principal.FindFirstValue(ClaimTypes.GivenName)
                           ?? principal.FindFirstValue(ClaimTypes.Name)
                           ?? "External";
            var lastName = principal.FindFirstValue(ClaimTypes.Surname) ?? "User";
            var fullName = $"{firstName} {lastName}".Trim();

            var user = _userService.GetUserByEmail(email);
            if (user == null)
            {
                user = new MediCare.Models.AppUser
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = string.Empty,
                    Password = Guid.NewGuid().ToString("N"),
                    Role = "Patient",
                    Status = "Approved"
                };
                _userService.AddUser(user);
            }

            if (user.Status != "Approved")
            {
                await HttpContext.SignOutAsync("External");
                TempData["Success"] = $"Your account is {user.Status}. Please wait for admin approval.";
                return RedirectToAction("Login");
            }

            string controller = user.Role switch
            {
                "Patient" => "User",
                "Admin" => "Admin",
                "Doctor" => "Doctor",
                "Staff" => "Staff",
                _ => "User"
            };

            await HttpContext.SignOutAsync("External");

            if (_userPreferenceService.IsTwoFactorEnabled(user.Email))
            {
                StartLoginTwoFactor(user.Email, user.Role, fullName, controller, user.Id);
                return RedirectToAction(nameof(VerifyLoginOtp));
            }

            CompleteLogin(user.Role, fullName, user.Email, controller, user.Id);
            return RedirectToAction("Dashboard", controller);
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
                if (_userPreferenceService.IsTwoFactorEnabled(email))
                {
                    StartLoginTwoFactor(email, "Admin", "Super Admin", "Admin", 0);
                    return RedirectToAction("VerifyLoginOtp");
                }

                CompleteLogin("Admin", "Super Admin", email, "Admin", 0);
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

            string controller = user.Role switch
            {
                "Patient" => "User", // Patient role maps to UserController
                "Admin" => "Admin",
                "Doctor" => "Doctor",
                "Staff" => "Staff",
                _ => user.Role
            };

            if (_userPreferenceService.IsTwoFactorEnabled(user.Email))
            {
                StartLoginTwoFactor(user.Email, user.Role, $"{user.FirstName} {user.LastName}", controller, user.Id);
                return RedirectToAction("VerifyLoginOtp");
            }

            CompleteLogin(user.Role, $"{user.FirstName} {user.LastName}", user.Email, controller, user.Id);
            return RedirectToAction("Dashboard", controller);
        }

        // ===================== LOGOUT =====================

        public IActionResult Logout()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            var sessionId = HttpContext.Session.GetString("LoginSessionId");
            if (int.TryParse(sessionId, out var sid))
            {
                _loginSessionService.EndSession(sid);
            }
            HttpContext.Session.Clear();
            if (!string.IsNullOrEmpty(email))
            {
                _notificationService.AddNotification(new MediCare.Models.Notification
                {
                    UserEmail = email,
                    Title = "Logged Out",
                    Message = $"{email} logged out as {role}",
                    Type = "info",
                    CreatedAt = DateTime.UtcNow
                });
            }
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

            var user = _userService.GetUserByEmail(email.Trim().ToLower());
            if (user == null)
            {
                ViewBag.Error = "No account found for this email.";
                return View();
            }

            string otp = GenerateOtp();

            HttpContext.Session.SetString("ResetOTP", otp);
            HttpContext.Session.SetString("ResetOTPExpiryTicks", DateTime.UtcNow.AddMinutes(5).Ticks.ToString());
            HttpContext.Session.SetString("ResetEmail", user.Email);
            HttpContext.Session.SetString("ResetOtpVerified", "false");

            SendOtpEmail(user.Email, otp);

            return RedirectToAction("VerifyOTP");
        }

        // ===================== VERIFY OTP =====================

        [HttpGet]
        public IActionResult VerifyOTP()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("ResetEmail")))
            {
                return RedirectToAction("ForgotPassword");
            }
            return View();
        }

        [HttpPost]
        public IActionResult VerifyOTP(string? userOtp)
        {
            var sessionOtp = HttpContext.Session.GetString("ResetOTP");
            var otpExpiryRaw = HttpContext.Session.GetString("ResetOTPExpiryTicks");
            var resetEmail = HttpContext.Session.GetString("ResetEmail");

            if (string.IsNullOrEmpty(resetEmail) || string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(otpExpiryRaw))
            {
                return RedirectToAction("ForgotPassword");
            }

            if (!long.TryParse(otpExpiryRaw, out var ticks) || DateTime.UtcNow > new DateTime(ticks, DateTimeKind.Utc))
            {
                ClearResetPasswordOtp();
                ViewBag.Error = "OTP expired. Please request a new one.";
                return View();
            }

            if (userOtp == sessionOtp)
            {
                HttpContext.Session.SetString("ResetOtpVerified", "true");
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Invalid OTP!";
            return View();
        }

        [HttpGet]
        public IActionResult VerifyLoginOtp()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("PendingLoginEmail")))
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        public IActionResult VerifyLoginOtp(string? userOtp)
        {
            var sessionOtp = HttpContext.Session.GetString("LoginOTP");
            var pendingEmail = HttpContext.Session.GetString("PendingLoginEmail");
            var role = HttpContext.Session.GetString("PendingLoginRole");
            var name = HttpContext.Session.GetString("PendingLoginName");
            var controller = HttpContext.Session.GetString("PendingLoginController");
            var userId = HttpContext.Session.GetString("PendingLoginUserId");
            var otpExpiryRaw = HttpContext.Session.GetString("LoginOTPExpiryTicks");

            if (string.IsNullOrEmpty(pendingEmail) || string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(otpExpiryRaw))
            {
                return RedirectToAction("Login");
            }

            if (!long.TryParse(otpExpiryRaw, out var ticks) || DateTime.UtcNow > new DateTime(ticks, DateTimeKind.Utc))
            {
                ClearPendingLoginOtp();
                ViewBag.Error = "OTP expired. Please login again.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(userOtp) || userOtp.Trim() != sessionOtp)
            {
                ViewBag.Error = "Invalid OTP!";
                return View();
            }

            CompleteLogin(role ?? "Patient", name ?? "User", pendingEmail, controller ?? "User", int.TryParse(userId, out var uid) ? uid : 0);
            ClearPendingLoginOtp();
            return RedirectToAction("Dashboard", controller ?? "User");
        }

        // ===================== RESET PASSWORD =====================

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (HttpContext.Session.GetString("ResetOtpVerified") != "true" ||
                string.IsNullOrEmpty(HttpContext.Session.GetString("ResetEmail")))
            {
                return RedirectToAction("ForgotPassword");
            }
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

            if (string.IsNullOrEmpty(email) || HttpContext.Session.GetString("ResetOtpVerified") != "true")
            {
                return RedirectToAction("ForgotPassword");
            }

            _userService.UpdateUserPassword(email, newPassword);

            ClearResetPasswordOtp();

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

        private void StartLoginTwoFactor(string email, string role, string name, string controller, int userId)
        {
            string otp = GenerateOtp();
            HttpContext.Session.SetString("LoginOTP", otp);
            HttpContext.Session.SetString("LoginOTPExpiryTicks", DateTime.UtcNow.AddMinutes(5).Ticks.ToString());
            HttpContext.Session.SetString("PendingLoginEmail", email);
            HttpContext.Session.SetString("PendingLoginRole", role);
            HttpContext.Session.SetString("PendingLoginName", name);
            HttpContext.Session.SetString("PendingLoginController", controller);
            HttpContext.Session.SetString("PendingLoginUserId", userId.ToString());
            SendLoginOtpEmail(email, otp);
        }

        private void CompleteLogin(string role, string name, string email, string controller, int userId)
        {
            if (userId > 0)
            {
                HttpContext.Session.SetString("UserId", userId.ToString());
            }
            SetSession(role, name, email);
            var record = _loginSessionService.StartSession(email, name, role);
            HttpContext.Session.SetString("LoginSessionId", record.Id.ToString());
            HttpContext.Session.SetString("LoginAt", record.LoginAt.ToString("O"));
            AddLoginNotification(email, role);
        }

        private void ClearPendingLoginOtp()
        {
            HttpContext.Session.Remove("LoginOTP");
            HttpContext.Session.Remove("LoginOTPExpiryTicks");
            HttpContext.Session.Remove("PendingLoginEmail");
            HttpContext.Session.Remove("PendingLoginRole");
            HttpContext.Session.Remove("PendingLoginName");
            HttpContext.Session.Remove("PendingLoginController");
            HttpContext.Session.Remove("PendingLoginUserId");
            HttpContext.Session.Remove("LoginSessionId");
        }

        private void AddLoginNotification(string email, string role)
        {
            _notificationService.AddNotification(new MediCare.Models.Notification
            {
                UserEmail = email,
                Title = "Logged In",
                Message = $"{email} logged in as {role}",
                Type = "success",
                CreatedAt = DateTime.UtcNow
            });
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

        private void ClearResetPasswordOtp()
        {
            HttpContext.Session.Remove("ResetOTP");
            HttpContext.Session.Remove("ResetOTPExpiryTicks");
            HttpContext.Session.Remove("ResetEmail");
            HttpContext.Session.Remove("ResetOtpVerified");
        }

        private void SendLoginOtpEmail(string email, string otp)
        {
            string subject = "MediCare Login OTP Verification";

            string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family:Segoe UI;background:#f4f6f9;padding:30px;'>
<div style='max-width:600px;margin:auto;background:white;border-radius:10px;padding:30px;text-align:center;'>
<h2 style='color:#16a34a;'>MediCare+</h2>
<h3>2FA Login Verification</h3>
<p>Use this OTP to complete your sign in.</p>
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
