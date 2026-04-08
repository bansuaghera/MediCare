using Microsoft.AspNetCore.Mvc;
using MediCare.Services;
using MediCare.Models;
using MediCare.Utilities;
using System.IO;

namespace MediCare.Controllers
{
    public class StaffController : Controller
    {
        private readonly PatientService _patientService;
        private readonly AppointmentService _appointmentService;
        private readonly UserService _userService;
        private readonly EmailService _emailService;
        private readonly DoctorService _doctorService;
        private readonly QueueService _queueService;
        private readonly NotificationService _notificationService;
        private readonly FeedbackService _feedbackService;
        private readonly UserPreferenceService _userPreferenceService;

        public StaffController(PatientService patientService, AppointmentService appointmentService, UserService userService, EmailService emailService, DoctorService doctorService, QueueService queueService, NotificationService notificationService, FeedbackService feedbackService, UserPreferenceService userPreferenceService)
        {
            _patientService = patientService;
            _appointmentService = appointmentService;
            _userService = userService;
            _emailService = emailService;
            _doctorService = doctorService;
            _queueService = queueService;
            _notificationService = notificationService;
            _feedbackService = feedbackService;
            _userPreferenceService = userPreferenceService;
        }

        private void LogStaffActivity(string title, string message, string type = "info")
        {
            var email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email)) return;

            _notificationService.AddNotification(new Notification
            {
                UserEmail = email,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            });
        }

        public IActionResult Dashboard()
        {
            var today = DateTime.Today;
            var appointments = _appointmentService.GetAllAppointments();
            var statusCounts = AnalyticsHelper.BuildStatusCounts(appointments.Where(a => a.AppointmentDate.Date == today));
            var dailyTrend = AnalyticsHelper.BuildDailyAppointmentTrend(appointments, 7);

            ViewBag.TodayCount = appointments.Count(a => a.AppointmentDate.Date == today);
            ViewBag.WaitingCount = appointments.Count(a => a.AppointmentDate.Date == today && (a.Status == "Waiting" || a.Status == "Scheduled"));
            ViewBag.InProgressCount = appointments.Count(a => a.AppointmentDate.Date == today && a.Status == "In Progress");
            ViewBag.CompletedCount = appointments.Count(a => a.AppointmentDate.Date == today && a.Status == "Completed");
            ViewBag.TotalPatients = _patientService.GetAllPatients().Count;
            ViewBag.TotalDoctors = _doctorService.GetAllDoctors().Count;
            ViewBag.ChartLabels = statusCounts.Labels;
            ViewBag.ChartData = statusCounts.Counts;
            ViewBag.DailyTrendLabels = dailyTrend.Labels;
            ViewBag.DailyTrendData = dailyTrend.Counts;

            ViewBag.RecentAppointments = appointments
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .ToList();

            return View();
        }

        public IActionResult Analytics()
        {
            var appointments = _appointmentService.GetAllAppointments();
            var today = DateTime.Today;
            var dailyTrend = AnalyticsHelper.BuildDailyAppointmentTrend(appointments, 14);
            var statusCounts = AnalyticsHelper.BuildStatusCounts(appointments.Where(a => a.AppointmentDate.Date == today));

            ViewBag.TodayCount = appointments.Count(a => a.AppointmentDate.Date == today);
            ViewBag.WaitingCount = appointments.Count(a => a.AppointmentDate.Date == today && (a.Status == "Waiting" || a.Status == "Scheduled"));
            ViewBag.InProgressCount = appointments.Count(a => a.AppointmentDate.Date == today && a.Status == "In Progress");
            ViewBag.CompletedCount = appointments.Count(a => a.AppointmentDate.Date == today && a.Status == "Completed");
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.ChartLabels = statusCounts.Labels;
            ViewBag.ChartData = statusCounts.Counts;
            ViewBag.DailyTrendLabels = dailyTrend.Labels;
            ViewBag.DailyTrendData = dailyTrend.Counts;
            ViewBag.RecentAppointments = appointments.OrderByDescending(a => a.AppointmentDate).Take(8).ToList();
            return View();
        }

        [HttpGet]
        public IActionResult RegisterPatient()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RegisterPatient(Patient patient)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(patient.Email))
                {
                    string randomPassword = PasswordGenerator.Generate();
                    var newUser = new AppUser
                    {
                        FirstName = patient.FirstName,
                        LastName = patient.LastName,
                        Email = patient.Email,
                        Phone = patient.Phone,
                        Password = randomPassword,
                        Role = "Patient",
                        Status = "Approved"
                    };

                    _userService.AddUser(newUser);

                    // Send Email
                    string subject = "Welcome to MediCare - Your Account Details";
                    string body = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2 style='color: #16a34a;'>Welcome {patient.FirstName}!</h2>
                            <p>Your account has been created successfully.</p>
                            <p><strong>Login Credentials:</strong></p>
                            <p>Email: {patient.Email}</p>
                            <p>Password: <strong>{randomPassword}</strong></p>
                            <p>Please log in to book appointments and view your medical history.</p>
                        </div>";
                    
                    _emailService.SendEmail(patient.Email, subject, body);
                }

                _patientService.AddPatient(patient);
                LogStaffActivity("Patient Registered", $"{patient.FirstName} {patient.LastName} was registered.", "success");
                return RedirectToAction("Patients");
            }
            return View(patient);
        }

        public IActionResult EditPatient(int id)
        {
            var patient = _patientService.GetPatientById(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [HttpPost]
        public IActionResult EditPatient(Models.Patient patient)
        {
            if (ModelState.IsValid)
            {
                _patientService.UpdatePatient(patient);
                LogStaffActivity("Patient Updated", $"{patient.FirstName} {patient.LastName} details were updated.", "info");
                return RedirectToAction("Patients");
            }
            return View(patient);
        }

        public IActionResult EditAppointment(int id)
        {
            return View();
        }

        [HttpPost]
        public IActionResult DeletePatient(int id)
        {
            _patientService.DeletePatient(id);
            LogStaffActivity("Patient Deleted", $"Patient record #{id} deleted.", "error");
            return RedirectToAction("Patients");
        }

        public IActionResult Patients()
        {
            var patients = _patientService.GetAllPatients();
            return View(patients);
        }
        public IActionResult GenerateToken()
        {
            return View();
        }
        public IActionResult Queue()
        {
            var appointments = _appointmentService.GetAllAppointments()
                .OrderByDescending(a => a.IsEmergency)
                .ThenBy(a => a.AppointmentDate)
                .ThenBy(a => a.TokenNumber)
                .ToList();

            // keep tokens unique & sequential in current priority order
            _appointmentService.NormalizeTokens(appointments);

            ViewBag.WaitingCount = appointments.Count(a => a.Status == "Waiting" || a.Status == "Scheduled");
            ViewBag.InProgressCount = appointments.Count(a => a.Status == "In Progress" || a.Status == "In-Progress");
            ViewBag.CompletedCount = appointments.Count(a => a.Status == "Completed");
            ViewBag.TotalToday = appointments.Count;

            return View(appointments);
        }
        public IActionResult CallToken(int? id = null)
        {
            var todayAppts = GetTodayCallQueue();
            var current = id.HasValue
                ? todayAppts.FirstOrDefault(a => a.Id == id.Value)
                : todayAppts.FirstOrDefault(a =>
                    a.Status == "Waiting" ||
                    a.Status == "Scheduled" ||
                    a.Status == "In Progress" ||
                    a.Status == "In-Progress") ?? todayAppts.FirstOrDefault();

            var next = current == null
                ? todayAppts.Take(5).ToList()
                : todayAppts.Where(a => a.Id != current.Id).Take(5).ToList();

            ViewBag.CurrentAppointment = current;
            ViewBag.NextAppointments = next;
            ViewBag.TotalToday = todayAppts.Count;
            return View(todayAppts);
        }

        [HttpPost]
        public IActionResult ServeToken(int doctorId, string tokenNumber)
        {
            _queueService.SetCurrentToken(doctorId, tokenNumber);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult CallTokenAction(int appointmentId, string action)
        {
            var appointment = _appointmentService.GetAppointmentById(appointmentId);
            if (appointment == null) return Json(new { success = false });

            action = (action ?? string.Empty).Trim().ToLowerInvariant();
            var queue = GetTodayCallQueue();

            if (action == "call")
            {
                appointment.Status = "Awaiting Confirmation";
                _appointmentService.UpdateAppointment(appointment);

                if (appointment.DoctorId > 0 && !string.IsNullOrWhiteSpace(appointment.TokenNumber))
                {
                    _queueService.SetCurrentToken(appointment.DoctorId, appointment.TokenNumber);
                }

                if (appointment.Patient?.Email != null)
                {
                    _notificationService.AddNotification(new Notification
                    {
                        UserEmail = appointment.Patient.Email,
                        Title = "Appointment Called",
                        Message = $"Token {appointment.TokenNumber} is waiting for your confirmation. Open Token Status to accept or reject.",
                        Type = "info",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                LogStaffActivity("Token Called", $"Token {appointment.TokenNumber} was called for appointment.", "info");
                return Json(new { success = true, action = "called" });
            }

            if (action == "complete")
            {
                appointment.Status = "Completed";
                _appointmentService.UpdateAppointment(appointment);

                if (appointment.DoctorId > 0)
                {
                    _queueService.ClearCurrentToken(appointment.DoctorId);
                }

                if (appointment.Patient?.Email != null)
                {
                    _notificationService.AddNotification(new Notification
                    {
                        UserEmail = appointment.Patient.Email,
                        Title = "Appointment Completed",
                        Message = $"Token {appointment.TokenNumber} has been marked completed.",
                        Type = "success",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                LogStaffActivity("Token Completed", $"Token {appointment.TokenNumber} marked completed.", "success");
                return Json(new { success = true, action = "completed" });
            }

            if (action == "repeat")
            {
                if (appointment.Patient?.Email != null)
                {
                    _notificationService.AddNotification(new Notification
                    {
                        UserEmail = appointment.Patient.Email,
                        Title = "Appointment Recalled",
                        Message = $"Token {appointment.TokenNumber} is waiting for your confirmation again.",
                        Type = "warning",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                LogStaffActivity("Token Repeated", $"Token {appointment.TokenNumber} called again.", "warning");
                return Json(new { success = true, action = "repeated" });
            }

            if (action == "skip")
            {
                var orderedIds = queue.Where(a => a.Id != appointmentId).Select(a => a.Id).ToList();
                orderedIds.Add(appointmentId);
                _appointmentService.UpdateSortOrder(orderedIds);
                if (appointment.DoctorId > 0)
                {
                    _queueService.ClearCurrentToken(appointment.DoctorId);
                }
                LogStaffActivity("Token Skipped", $"Token {appointment.TokenNumber} moved to the end of the queue.", "warning");
                return Json(new { success = true, action = "skipped" });
            }

            return Json(new { success = false });
        }
        public IActionResult Schedule()
        {
            return View();
        }
        public IActionResult Appointments()
        {
            var appointments = _appointmentService.GetAllAppointments()
                .OrderByDescending(a => a.Status == "In Progress" || a.Status == "In-Progress")
                .ThenByDescending(a => a.IsEmergency)
                .ThenBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeSlot)
                .ToList();
            
            ViewBag.Doctors = _doctorService.GetAllDoctors();
            ViewBag.Patients = _patientService.GetAllPatients();
            return View(appointments);
        }

        [HttpGet]
        public IActionResult TodayAppointments()
        {
            var today = DateTime.Today;
            var appointments = _appointmentService.GetAllAppointments()
                .Where(a => a.AppointmentDate.Date == today ||
                            a.Status == "Waiting" ||
                            a.Status == "Scheduled" ||
                            a.Status == "In Progress" ||
                            a.Status == "In-Progress" ||
                            a.Status == "Completed" ||
                            a.Status == "Cancelled" ||
                            a.Status == "No-Show")
                .OrderByDescending(a => a.Status == "In Progress" || a.Status == "In-Progress")
                .ThenByDescending(a => a.IsEmergency)
                .ThenBy(a => a.AppointmentDate)
                .ToList();

            return View(appointments);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment != null)
            {
                // Normalize status spelling so all panels stay consistent
                appointment.Status = status.Replace("-", " ");
                _appointmentService.UpdateAppointment(appointment);
                if (string.Equals(appointment.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    _queueService.ClearCurrentToken(appointment.DoctorId);
                }
                LogStaffActivity("Appointment Status Updated", $"Appointment #{id} status changed to {appointment.Status}.", "info");
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteAppointment(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment != null)
            {
                _queueService.ClearCurrentToken(appointment.DoctorId);
                _appointmentService.DeleteAppointment(id);
            }
            LogStaffActivity("Appointment Deleted", $"Appointment #{id} deleted.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult BulkDeleteAppointments([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            foreach (var id in ids)
            {
                var appointment = _appointmentService.GetAppointmentById(id);
                if (appointment != null)
                {
                    _queueService.ClearCurrentToken(appointment.DoctorId);
                    _appointmentService.DeleteAppointment(id);
                }
            }
            LogStaffActivity("Appointments Deleted", $"{ids.Length} appointment(s) deleted.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateOrder([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            _appointmentService.UpdateSortOrder(ids);
            LogStaffActivity("Queue Reordered", $"{ids.Length} appointment(s) reordered.", "info");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult PromoteSelectedAppointments([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });

            var selectedIds = ids.Distinct().ToList();
            var orderedAppointments = _appointmentService.GetAllAppointments();
            var promoted = orderedAppointments
                .Where(a => selectedIds.Contains(a.Id))
                .OrderBy(a => selectedIds.IndexOf(a.Id))
                .Select(a => a.Id)
                .ToList();

            var remaining = orderedAppointments
                .Where(a => !selectedIds.Contains(a.Id))
                .Select(a => a.Id);

            _appointmentService.UpdateSortOrder(promoted.Concat(remaining).ToList());
            LogStaffActivity("Appointments Prioritized", $"{promoted.Count} appointment(s) moved to top priority.", "info");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteAllAppointments()
        {
            var appointments = _appointmentService.GetAllAppointments();
            foreach (var app in appointments)
            {
                _queueService.ClearCurrentToken(app.DoctorId);
                _appointmentService.DeleteAppointment(app.Id);
            }
            LogStaffActivity("Appointments Cleared", "All appointments removed by staff.", "error");
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetAppointmentDetails(int id)
        {
            var app = _appointmentService.GetAppointmentById(id);
            if (app == null) return NotFound();

            return Json(new {
                id = app.Id,
                patientName = app.Patient?.FirstName + " " + app.Patient?.LastName,
                doctorName = "Dr. " + app.Doctor?.FirstName + " " + app.Doctor?.LastName,
                date = app.AppointmentDate.ToString("yyyy-MM-dd"),
                time = app.TimeSlot,
                token = app.TokenNumber,
                status = app.Status,
                subject = app.Subject,
                notes = app.Notes
            });
        }
        public IActionResult BookAppointment()
        {
            ViewBag.Doctors = _doctorService.GetAllDoctors();
            ViewBag.Patients = _patientService.GetAllPatients();
            return View();
        }

        [HttpPost]
        public IActionResult BookAppointment(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                appointment.Status = "Scheduled";
                appointment.AppointmentDate = DateTime.Today; // Staff books for today by default or specified

                _appointmentService.AddAppointment(appointment);
                LogStaffActivity("Appointment Booked", $"Appointment booked for patient #{appointment.PatientId}.", "success");
                return RedirectToAction("TodayAppointments");
            }
            ViewBag.Doctors = _doctorService.GetAllDoctors();
            ViewBag.Patients = _patientService.GetAllPatients();
            return View(appointment);
        }

        [HttpGet]
        public IActionResult Print()
        {
            var appointments = _appointmentService.GetAllAppointments();
            var today = DateTime.Today;

            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.WaitingCount = appointments.Count(a => a.AppointmentDate.Date == today && (a.Status == "Waiting" || a.Status == "Scheduled"));
            ViewBag.InProgressCount = appointments.Count(a => a.AppointmentDate.Date == today && a.Status == "In Progress");
            ViewBag.CompletedCount = appointments.Count(a => a.AppointmentDate.Date == today && a.Status == "Completed");
            ViewBag.EmergencyCount = appointments.Count(a => a.IsEmergency);

            return View(appointments);
        }

        public IActionResult PrintAppointment(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment == null) return NotFound();
            return View(appointment);
        }

        public IActionResult Profile()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var user = _userService.GetUserByEmail(email);
            var pref = _userPreferenceService.GetOrCreate(email);
            ViewBag.PushNotificationsEnabled = pref.PushNotificationsEnabled;
            ViewBag.TwoFactorEnabled = pref.TwoFactorEnabled;
            ViewBag.ProfilePhotoUrl = GetStaffPhotoUrl(email);
            return View(user);
        }

        [HttpPost]
        public IActionResult Profile(AppUser form, IFormFile? photo)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email)) return RedirectToAction("Login", "Login");

            var user = _userService.GetUserByEmail(email);
            if (user == null) return RedirectToAction("Login", "Login");

            user.FirstName = form.FirstName;
            user.LastName = form.LastName;
            user.Phone = form.Phone;

            if (photo != null && photo.Length > 0)
            {
                SaveStaffPhoto(email, photo);
            }

            _notificationService.AddNotification(new Notification
            {
                UserEmail = email,
                Title = "Profile Updated",
                Message = "Your staff profile was updated successfully.",
                Type = "success",
                CreatedAt = DateTime.UtcNow
            });
            TempData["ToastMessage"] = "Profile updated successfully";
            TempData["ToastType"] = "success";
            return RedirectToAction("Profile");
        }

        private string? GetStaffPhotoUrl(string email)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) return null;

            var prefix = GetStaffPhotoPrefix(email);
            var file = Directory.GetFiles(uploads, $"{prefix}.*").FirstOrDefault();
            return file == null ? null : $"/uploads/{Path.GetFileName(file)}";
        }

        private void SaveStaffPhoto(string email, IFormFile photo)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var prefix = GetStaffPhotoPrefix(email);
            foreach (var existing in Directory.GetFiles(uploads, $"{prefix}.*"))
            {
                System.IO.File.Delete(existing);
            }

            var extension = Path.GetExtension(photo.FileName);
            var fileName = $"{prefix}{extension}";
            var path = Path.Combine(uploads, fileName);
            using var stream = System.IO.File.Create(path);
            photo.CopyTo(stream);
        }

        private static string GetStaffPhotoPrefix(string email)
        {
            var safeEmail = new string((email ?? "staff").Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(safeEmail)) safeEmail = "staff";
            return $"staff_{safeEmail}";
        }

        [HttpPost]
        public IActionResult TogglePushNotifications(bool enabled)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });
            _userPreferenceService.SetPushEnabled(email, enabled);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ToggleTwoFactor(bool enabled)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });
            _userPreferenceService.SetTwoFactorEnabled(email, enabled);
            return Json(new { success = true });
        }

        public IActionResult History()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var list = _notificationService.GetAllNotificationsForUser(email);
            ViewBag.DeleteAllUrl = "/Staff/DeleteAllNotifications";
            ViewBag.DeleteUrl = "/Staff/DeleteNotifications";
            ViewBag.TitleText = "Activity History";
            ViewBag.SubtitleText = "All staff activities stored here";
            return View(list);
        }

        private List<Appointment> GetTodayCallQueue()
        {
            return _appointmentService.GetAllAppointments()
                .Where(a => a.AppointmentDate.Date == DateTime.Today &&
                    a.Status != "Cancelled" &&
                    a.Status != "No-Show")
                .OrderByDescending(a => a.IsEmergency)
                .ThenBy(a => a.SortOrder)
                .ThenBy(a => a.AppointmentDate)
                .ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Feedback()
        {
            string name = HttpContext.Session.GetString("UserName") ?? "Staff";
            var history = await _feedbackService.GetFeedbacksByNameAsync(name);
            return View(history);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(Feedback feedback, string? subject)
        {
            string name = HttpContext.Session.GetString("UserName") ?? "Staff";
            feedback.PatientId = null;
            feedback.PatientName = name;
            feedback.SubmittedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            if (!string.IsNullOrWhiteSpace(subject))
            {
                feedback.Comment = $"[{subject.Trim()}] {feedback.Comment}";
            }
            await _feedbackService.AddFeedbackAsync(feedback);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            string name = HttpContext.Session.GetString("UserName") ?? "Staff";
            var fb = await _feedbackService.GetFeedbackByIdAsync(id);
            if (fb == null || !string.Equals(fb.PatientName, name, StringComparison.OrdinalIgnoreCase)) return Json(new { success = false });
            await _feedbackService.DeleteFeedbackAsync(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedFeedbacks([FromBody] int[] ids)
        {
            string name = HttpContext.Session.GetString("UserName") ?? "Staff";
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            var history = await _feedbackService.GetFeedbacksByNameAsync(name);
            var allowedIds = history.Where(f => ids.Contains(f.Id)).Select(f => f.Id).ToList();
            if (allowedIds.Count == 0) return Json(new { success = false });
            await _feedbackService.DeleteFeedbacksAsync(allowedIds);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllFeedbacks()
        {
            string name = HttpContext.Session.GetString("UserName") ?? "Staff";
            await _feedbackService.DeleteAllForNameAsync(name);
            return Json(new { success = true });
        }

        public IActionResult Notifications()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var list = string.IsNullOrEmpty(email) ? new List<Notification>() : _notificationService.GetNotificationsForUser(email);
            if (!string.IsNullOrEmpty(email)) _notificationService.MarkAllRead(email);
            return View(list);
        }

        [HttpPost]
        public IActionResult DeleteAllNotifications()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });
            _notificationService.DeleteAllForUser(email);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteNotifications([FromBody] int[] ids)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email) || ids == null) return Json(new { success = false });
            foreach (var id in ids) _notificationService.DeleteNotification(id, email);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult MarkAllNotificationsRead()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });
            _notificationService.MarkAllRead(email);
            return Json(new { success = true });
        }
    }
}
