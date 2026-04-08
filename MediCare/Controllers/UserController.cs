using Microsoft.AspNetCore.Mvc;
using MediCare.Models;
using MediCare.Services;
using MediCare.Utilities;

namespace MediCare.Controllers
{
    public class UserController : Controller
    {
        private readonly AppointmentService _appointmentService;
        private readonly PatientService _patientService;
        private readonly DoctorService _doctorService;
        private readonly UserService _userService;
        private readonly PrescriptionService _prescriptionService;
        private readonly FeedbackService _feedbackService;
        private readonly NotificationService _notificationService;
        private readonly UserPreferenceService _userPreferenceService;

        public UserController(AppointmentService appointmentService, PatientService patientService, DoctorService doctorService, UserService userService, PrescriptionService prescriptionService, FeedbackService feedbackService, NotificationService notificationService, UserPreferenceService userPreferenceService)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
            _doctorService = doctorService;
            _userService = userService;
            _prescriptionService = prescriptionService;
            _feedbackService = feedbackService;
            _notificationService = notificationService;
            _userPreferenceService = userPreferenceService;
        }

        public IActionResult Dashboard()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patients = _patientService.GetAllPatients();
            var patient = patients.FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());
            
            if (patient == null)
            {
                var user = _userService.GetUserByEmail(email);
                if (user != null)
                {
                    patient = new MediCare.Models.Patient
                    {
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Phone = user.Phone,
                        DateOfBirth = DateTime.SpecifyKind(DateTime.Now.AddYears(-20), DateTimeKind.Utc), // Default DOB
                        Gender = "Not Specified",
                        BloodGroup = "Not Specified",
                        Address = "Not Specified",
                        MedicalHistory = "None"
                    };
                    _patientService.AddPatient(patient);
                }
            }
            
            var appointments = new List<MediCare.Models.Appointment>();
            if (patient != null)
            {
                appointments = _appointmentService.GetAllAppointments()
                    .Where(a => a.PatientId == patient.Id)
                    .OrderBy(a => a.AppointmentDate)
                    .ToList();
            }

            var dailyTrend = AnalyticsHelper.BuildDailyAppointmentTrend(appointments, 7);
            var statusCounts = AnalyticsHelper.BuildStatusCounts(appointments);
            ViewBag.ChartLabels = dailyTrend.Labels;
            ViewBag.ChartData = dailyTrend.Counts;
            ViewBag.StatusLabels = statusCounts.Labels;
            ViewBag.StatusData = statusCounts.Counts;

            return View(appointments);
        }

        public IActionResult Analytics()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower())
                ?? _patientService.GetAllPatients().FirstOrDefault();

            var appointments = new List<MediCare.Models.Appointment>();
            if (patient != null)
            {
                appointments = _appointmentService.GetAllAppointments()
                    .Where(a => a.PatientId == patient.Id)
                    .OrderBy(a => a.AppointmentDate)
                    .ToList();
            }

            var dailyTrend = AnalyticsHelper.BuildDailyAppointmentTrend(appointments, 14);
            var statusCounts = AnalyticsHelper.BuildStatusCounts(appointments);
            ViewBag.ChartLabels = dailyTrend.Labels;
            ViewBag.ChartData = dailyTrend.Counts;
            ViewBag.StatusLabels = statusCounts.Labels;
            ViewBag.StatusData = statusCounts.Counts;
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.UpcomingAppointments = appointments.Count(a => a.AppointmentDate.Date >= DateTime.Today);
            ViewBag.PastAppointments = appointments.Count(a => a.AppointmentDate.Date < DateTime.Today);
            ViewBag.RecentAppointments = appointments.OrderByDescending(a => a.AppointmentDate).Take(8).ToList();
            return View();
        }
        public IActionResult Doctors()
        {
            var doctors = _doctorService.GetAllDoctors();
            return View(doctors);
        }

        public IActionResult BookAppointment()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var doctors = _doctorService.GetAllDoctors();
            ViewBag.Doctors = doctors;
            return View();
        }

        [HttpPost]
        public IActionResult BookAppointment(MediCare.Models.Appointment appointment)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            if (patient == null)
            {
                // Auto-create patient record for the registered user
                var user = _userService.GetUserByEmail(email);
                if (user != null)
                {
                    patient = new MediCare.Models.Patient
                    {
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Phone = user.Phone,
                        DateOfBirth = DateTime.SpecifyKind(DateTime.Now.AddYears(-20), DateTimeKind.Utc), // Default DOB
                        Gender = "Not Specified",
                        BloodGroup = "Not Specified",
                        Address = "Not Specified",
                        MedicalHistory = "None"
                    };
                    _patientService.AddPatient(patient);
                }
            }

            if (patient != null)
            {
                appointment.PatientId = patient.Id;
                appointment.Status = "Scheduled";

                // If emergency, flag it so queue floats it to the top
                // appointment.IsEmergency is already bound from checkbox; ensure boolean defaults to false
                appointment.IsEmergency = appointment.IsEmergency;

                // Ensure the date is UTC for PostgreSQL
                appointment.AppointmentDate = DateTime.SpecifyKind(appointment.AppointmentDate, DateTimeKind.Utc);
                
                _appointmentService.AddAppointment(appointment);
                _notificationService.AddNotification(new Notification
                {
                    UserEmail = email,
                    Title = "Appointment Booked",
                    Message = $"Appointment booked on {appointment.AppointmentDate:yyyy-MM-dd} at {appointment.TimeSlot}",
                    Type = appointment.IsEmergency ? "warning" : "info",
                    CreatedAt = DateTime.UtcNow
                });
                return RedirectToAction("MyAppointments");
            }
            
            ViewBag.Error = "You are not a registered patient.";
            ViewBag.Doctors = _doctorService.GetAllDoctors();
            return View(appointment);
        }
        public IActionResult EditAppointment(int id)
        {
            return View();
        }

        public IActionResult MyAppointments()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patients = _patientService.GetAllPatients();
            var patient = patients.FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            var appointments = new List<MediCare.Models.Appointment>();
            if (patient != null)
            {
                appointments = _appointmentService.GetAllAppointments()
                    .Where(a => a.PatientId == patient.Id)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToList();
            }

            return View(appointments);
        }

        public IActionResult TokenStatus()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patients = _patientService.GetAllPatients();
            var patient = patients.FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            var appointments = new List<MediCare.Models.Appointment>();
            if (patient != null)
            {
                appointments = _appointmentService.GetAllAppointments()
                    .Where(a => a.PatientId == patient.Id && (
                        a.Status == "Waiting" ||
                        a.Status == "Scheduled" ||
                        a.Status == "Awaiting Confirmation" ||
                        a.Status == "In Progress"))
                    .OrderBy(a => a.AppointmentDate)
                    .ToList();
            }

            return View(appointments);
        }

        [HttpPost]
        public IActionResult RespondToToken(int id, string action)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });

            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());
            if (patient == null) return Json(new { success = false });

            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment == null || appointment.PatientId != patient.Id) return Json(new { success = false });

            action = (action ?? string.Empty).Trim().ToLowerInvariant();
            if (action == "accept")
            {
                appointment.Status = "In Progress";
                _appointmentService.UpdateAppointment(appointment);
                _notificationService.AddNotification(new Notification
                {
                    UserEmail = email,
                    Title = "Token Accepted",
                    Message = $"You accepted token {appointment.TokenNumber}. It is now in progress.",
                    Type = "success",
                    CreatedAt = DateTime.UtcNow
                });
                return Json(new { success = true, action = "accepted", status = appointment.Status });
            }

            if (action == "reject")
            {
                var queue = _appointmentService.GetAllAppointments()
                    .Where(a => a.DoctorId == appointment.DoctorId && a.AppointmentDate.Date == appointment.AppointmentDate.Date && a.Status != "Cancelled" && a.Status != "Completed")
                    .OrderByDescending(a => a.IsEmergency)
                    .ThenBy(a => a.SortOrder)
                    .ThenBy(a => a.AppointmentDate)
                    .ToList();

                var orderedIds = queue.Where(a => a.Id != appointment.Id).Select(a => a.Id).ToList();
                orderedIds.Add(appointment.Id);
                _appointmentService.UpdateSortOrder(orderedIds);
                appointment.Status = "Scheduled";
                _appointmentService.UpdateAppointment(appointment);
                _notificationService.AddNotification(new Notification
                {
                    UserEmail = email,
                    Title = "Token Rejected",
                    Message = $"You rejected token {appointment.TokenNumber}. It was moved back in the queue.",
                    Type = "warning",
                    CreatedAt = DateTime.UtcNow
                });
                return Json(new { success = true, action = "rejected", status = appointment.Status });
            }

            return Json(new { success = false });
        }

        public IActionResult History()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patients = _patientService.GetAllPatients();
            var patient = patients.FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            var appointments = new List<MediCare.Models.Appointment>();
            if (patient != null)
            {
                appointments = _appointmentService.GetAllAppointments()
                    .Where(a => a.PatientId == patient.Id && (a.Status == "Completed" || a.AppointmentDate < DateTime.Today))
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToList();
            }

            ViewBag.ActivityHistory = _notificationService.GetAllNotificationsForUser(email);

            return View(appointments);
        }

        public async Task<IActionResult> Prescriptions()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patients = _patientService.GetAllPatients();
            var patient = patients.FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            var prescriptions = new List<MediCare.Models.Prescription>();
            if (patient != null)
            {
                var allPrescriptions = await _prescriptionService.GetAllPrescriptionsAsync();
                prescriptions = allPrescriptions.Where(p => p.PatientId == patient.Id).ToList();
            }

            return View(prescriptions);
        }

        public IActionResult Profile()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());
            var pref = _userPreferenceService.GetOrCreate(email);
            ViewBag.PushNotificationsEnabled = pref.PushNotificationsEnabled;
            ViewBag.TwoFactorEnabled = pref.TwoFactorEnabled;
            return View(patient);
        }

        [HttpPost]
        public IActionResult Profile(Patient form, IFormFile? photo)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());
            if (patient == null) return RedirectToAction("Login", "Login");

            patient.FirstName = form.FirstName;
            patient.LastName = form.LastName;
            patient.Phone = form.Phone;
            patient.Address = form.Address;
            patient.BloodGroup = form.BloodGroup;
            patient.MedicalHistory = form.MedicalHistory;
            patient.Gender = form.Gender;
            patient.DateOfBirth = form.DateOfBirth;

            // Photo upload: save to wwwroot/uploads and stash path in TempData to show; skipped persistence to DB to avoid schema change
            if (photo != null && photo.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                var fileName = $"patient_{patient.Id}{Path.GetExtension(photo.FileName)}";
                var path = Path.Combine(uploads, fileName);
                using (var stream = System.IO.File.Create(path))
                {
                    photo.CopyTo(stream);
                }
                TempData["ProfilePhoto"] = $"/uploads/{fileName}";
            }

            _patientService.UpdatePatient(patient);
            _notificationService.AddNotification(new Notification
            {
                UserEmail = email,
                Title = "Profile Updated",
                Message = "Your profile details were updated successfully.",
                Type = "success",
                CreatedAt = DateTime.UtcNow
            });
            TempData["ToastMessage"] = "Profile updated successfully";
            TempData["ToastType"] = "success";
            return RedirectToAction("Profile");
        }
        [HttpPost]
        public IActionResult CancelAppointment(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment != null)
            {
                appointment.Status = "Cancelled";
                _appointmentService.UpdateAppointment(appointment);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteAppointment(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            if (appointment != null && patient != null && appointment.PatientId == patient.Id)
            {
                _appointmentService.DeleteAppointment(id);
                _notificationService.AddNotification(new Notification
                {
                    UserEmail = email,
                    Title = "Appointment Deleted",
                    Message = $"Appointment #{id} deleted",
                    Type = "error",
                    CreatedAt = DateTime.UtcNow
                });
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteAllAppointments()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            if (patient != null)
            {
                var appointments = _appointmentService.GetAllAppointments().Where(a => a.PatientId == patient.Id).ToList();
                foreach (var appt in appointments)
                {
                    _appointmentService.DeleteAppointment(appt.Id);
                }
                _notificationService.AddNotification(new Notification
                {
                    UserEmail = email ?? "",
                    Title = "All Appointments Deleted",
                    Message = "All your appointments were deleted.",
                    Type = "error",
                    CreatedAt = DateTime.UtcNow
                });
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteSelectedAppointments([FromBody] int[] ids)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            if (patient == null || ids == null || ids.Length == 0)
            {
                return Json(new { success = false });
            }

            var appointments = _appointmentService.GetAllAppointments()
                .Where(a => ids.Contains(a.Id) && a.PatientId == patient.Id)
                .ToList();

            foreach (var appt in appointments)
            {
                _appointmentService.DeleteAppointment(appt.Id);
            }

            _notificationService.AddNotification(new Notification
            {
                UserEmail = email ?? "",
                Title = "Appointments Deleted",
                Message = $"{appointments.Count} selected appointments deleted.",
                Type = "error",
                CreatedAt = DateTime.UtcNow
            });
            return Json(new { success = true });
        }
        [HttpGet]
        public IActionResult GetAppointmentDetails(int id)
        {
            var appointment = _appointmentService.GetAllAppointments().FirstOrDefault(a => a.Id == id);
            if (appointment == null) return NotFound();

            return Json(new {
                doctorName = "Dr. " + appointment.Doctor?.FirstName + " " + appointment.Doctor?.LastName,
                specialty = appointment.Doctor?.Specialty,
                date = appointment.AppointmentDate.ToString("yyyy-MM-dd"),
                time = appointment.TimeSlot,
                status = appointment.Status,
                token = appointment.TokenNumber,
                subject = appointment.Subject,
                notes = appointment.Notes
            });
        }
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(Feedback feedback, string? subject)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            feedback.SubmittedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            if (patient != null)
            {
                feedback.PatientId = patient.Id;
                feedback.PatientName = $"{patient.FirstName} {patient.LastName}".Trim();
            }
            else
            {
                feedback.PatientName = HttpContext.Session.GetString("UserName") ?? "User";
            }

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
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());
            var fb = await _feedbackService.GetFeedbackByIdAsync(id);
            if (patient == null || fb == null || fb.PatientId != patient.Id) return Json(new { success = false });
            await _feedbackService.DeleteFeedbackAsync(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedFeedbacks([FromBody] int[] ids)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());
            if (patient == null || ids == null || ids.Length == 0) return Json(new { success = false });

            var feedbacks = await _feedbackService.GetFeedbacksForPatientAsync(patient.Id);
            var allowedIds = feedbacks.Where(f => ids.Contains(f.Id)).Select(f => f.Id).ToList();
            if (allowedIds.Count == 0) return Json(new { success = false });

            await _feedbackService.DeleteFeedbacksAsync(allowedIds);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllFeedbacks()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());
            if (patient == null) return Json(new { success = false });
            await _feedbackService.DeleteAllForPatientAsync(patient.Id);
            return Json(new { success = true });
        }

        public IActionResult Notifications()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var notifications = string.IsNullOrEmpty(email)
                ? new List<Notification>()
                : _notificationService.GetNotificationsForUser(email);
            // Mark all read when opening the page
            if (!string.IsNullOrEmpty(email)) _notificationService.MarkAllRead(email);
            return View(notifications);
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

        public async Task<IActionResult> Feedback()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());
            var history = patient == null
                ? new List<Feedback>()
                : await _feedbackService.GetFeedbacksForPatientAsync(patient.Id);

            return View(history);
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}
