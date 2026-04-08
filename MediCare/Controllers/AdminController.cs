using Microsoft.AspNetCore.Mvc;
using MediCare.Services;
using MediCare.Models;
using MediCare.Utilities;
using System.IO;

namespace MediCare.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserService _userService;
        private readonly PatientService _patientService;
        private readonly DoctorService _doctorService;
        private readonly MedicineService _medicineService;
        private readonly AppointmentService _appointmentService;
        private readonly EmailService _emailService;
        private readonly PrescriptionService _prescriptionService;
        private readonly QueueService _queueService;
        private readonly FeedbackService _feedbackService;
        private readonly OPDScheduleService _opdScheduleService;
        private readonly NotificationService _notificationService;
        private readonly UserPreferenceService _userPreferenceService;

        public AdminController(UserService userService, PatientService patientService, DoctorService doctorService, MedicineService medicineService, AppointmentService appointmentService, EmailService emailService, PrescriptionService prescriptionService, QueueService queueService, FeedbackService feedbackService, OPDScheduleService opdScheduleService, NotificationService notificationService, UserPreferenceService userPreferenceService)
        {
            _userService = userService;
            _patientService = patientService;
            _doctorService = doctorService;
            _medicineService = medicineService;
            _appointmentService = appointmentService;
            _emailService = emailService;
            _prescriptionService = prescriptionService;
            _queueService = queueService;
            _feedbackService = feedbackService;
            _opdScheduleService = opdScheduleService;
            _notificationService = notificationService;
            _userPreferenceService = userPreferenceService;
        }

        private void LogAdminActivity(string title, string message, string type = "info")
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
            var appointments = _appointmentService.GetAllAppointments();
            var dailyAppointments = AnalyticsHelper.BuildDailyAppointmentTrend(appointments, 7);
            var dailyPatients = AnalyticsHelper.BuildDailyUniquePatientTrend(appointments, 7);
            var model = new MediCare.Models.ViewModels.AdminDashboardViewModel
            {
                TotalPatients = _patientService.GetAllPatients().Count,
                ActiveDoctors = _doctorService.GetAllDoctors().Count,
                AppointmentsToday = appointments.Count(a => a.AppointmentDate.Date == DateTime.Today),
                TotalRevenue = _doctorService.GetAllDoctors().Sum(d => d.ConsultationFee),
                RecentAppointments = appointments.OrderByDescending(a => a.AppointmentDate).Take(5).ToList(),
                ChartLabels = dailyAppointments.Labels,
                WeeklyPatients = dailyPatients.Counts,
                WeeklyAppointments = dailyAppointments.Counts
            };
            return View(model);
        }

        public IActionResult Analytics()
        {
            var appointments = _appointmentService.GetAllAppointments();
            var appointmentTrend = AnalyticsHelper.BuildDailyAppointmentTrend(appointments, 14);
            var statusCounts = AnalyticsHelper.BuildStatusCounts(appointments);

            ViewBag.TotalPatients = _patientService.GetAllPatients().Count;
            ViewBag.ActiveDoctors = _doctorService.GetAllDoctors().Count;
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.EmergencyCount = appointments.Count(a => a.IsEmergency);
            ViewBag.ChartLabels = appointmentTrend.Labels;
            ViewBag.ChartData = appointmentTrend.Counts;
            ViewBag.StatusLabels = statusCounts.Labels;
            ViewBag.StatusData = statusCounts.Counts;
            ViewBag.RecentAppointments = appointments.OrderByDescending(a => a.AppointmentDate).Take(8).ToList();
            return View();
        }
        public IActionResult Patient()
        {
            var patients = _patientService.GetAllPatients();
            return View(patients);
        }
        public IActionResult Doctors()
        {
            var doctors = _doctorService.GetAllDoctors();
            return View(doctors);
        }
        public IActionResult Staff()
        {
            var staff = _userService.GetStaffUsers();
            return View(staff);
        }
        public async Task<IActionResult> OPDSchedule()
        {
            var schedules = await _opdScheduleService.GetAllSchedulesAsync();
            return View(schedules);
        }

        public async Task<IActionResult> Feedbacks()
        {
            var feedbacks = await _feedbackService.GetAllFeedbacksAsync();
            return View(feedbacks);
        }

        public IActionResult AddOPDSchedule()
        {
            ViewBag.Doctors = _doctorService.GetAllDoctors();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddOPDSchedule(OPDSchedule schedule)
        {
            if (ModelState.IsValid)
            {
                await _opdScheduleService.AddScheduleAsync(schedule);
                LogAdminActivity("OPD Schedule Added", $"Schedule for Dr. {schedule.DoctorId} added.", "success");
                return RedirectToAction("OPDSchedule");
            }
            ViewBag.Doctors = _doctorService.GetAllDoctors();
            return View(schedule);
        }

        public IActionResult Appointments()
        {
            var appointments = _appointmentService.GetAllAppointments();
            return View(appointments);
        }

        public async Task<IActionResult> Prescriptions()
        {
            var prescriptions = await _prescriptionService.GetAllPrescriptionsAsync();
            return View(prescriptions);
        }

        public IActionResult TokenQueue()
        {
            var appointments = _appointmentService.GetAllAppointments()
                .Where(a => a.AppointmentDate.Date == DateTime.Today &&
                            a.Status != "Cancelled" &&
                            a.Status != "No-Show")
                .OrderByDescending(a => a.Status == "In Progress" || a.Status == "In-Progress")
                .ThenByDescending(a => a.IsEmergency)
                .ThenBy(a => a.SortOrder)
                .ThenBy(a => a.AppointmentDate)
                .ToList();

            ViewBag.CurrentTokens = _queueService.GetAllCurrentTokens();
            ViewBag.WaitingCount = appointments.Count(a => a.Status == "Waiting" || a.Status == "Scheduled");
            ViewBag.InProgressCount = appointments.Count(a => a.Status == "In Progress" || a.Status == "In-Progress");
            ViewBag.CompletedCount = appointments.Count(a => a.Status == "Completed");
            ViewBag.TotalToday = appointments.Count;
            return View(appointments);
        }

        [HttpPost]
        public IActionResult ServeToken(int doctorId, string tokenNumber)
        {
            _queueService.SetCurrentToken(doctorId, tokenNumber);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment == null) return Json(new { success = false });

            appointment.Status = status.Replace("-", " ");
            _appointmentService.UpdateAppointment(appointment);
            if (string.Equals(appointment.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                _queueService.ClearCurrentToken(appointment.DoctorId);
            }
            LogAdminActivity("Appointment Status Updated", $"Appointment #{id} status changed to {appointment.Status}.", "info");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteAppointment(int id)
        {
            var app = _appointmentService.GetAppointmentById(id);
            if (app == null) return Json(new { success = false });

            _appointmentService.DeleteAppointment(id);
            if (app.DoctorId > 0) _queueService.ClearCurrentToken(app.DoctorId);
            LogAdminActivity("Appointment Deleted", $"Appointment #{id} deleted.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult BulkDeleteAppointments([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });

            foreach (var id in ids)
            {
                var app = _appointmentService.GetAppointmentById(id);
                if (app != null)
                {
                    if (app.DoctorId > 0) _queueService.ClearCurrentToken(app.DoctorId);
                    _appointmentService.DeleteAppointment(id);
                }
            }

            LogAdminActivity("Appointments Deleted", $"{ids.Length} appointment record(s) deleted.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteAllAppointments()
        {
            var appointments = _appointmentService.GetAllAppointments();
            foreach (var app in appointments)
            {
                _appointmentService.DeleteAppointment(app.Id);
                if (app.DoctorId > 0) _queueService.ClearCurrentToken(app.DoctorId);
            }
            LogAdminActivity("Appointments Cleared", "All appointments removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateOrder([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            _appointmentService.UpdateSortOrder(ids.ToList());
            LogAdminActivity("Queue Reordered", $"Queue order updated for {ids.Length} appointment(s).", "info");
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetAppointmentDetails(int id)
        {
            var app = _appointmentService.GetAppointmentById(id);
            if (app == null) return NotFound();
            return Json(new
            {
                token = app.TokenNumber,
                patientName = $"{app.Patient?.FirstName} {app.Patient?.LastName}".Trim(),
                doctorName = $"Dr. {app.Doctor?.FirstName} {app.Doctor?.LastName}".Trim(),
                date = app.AppointmentDate.ToString("yyyy-MM-dd"),
                time = app.TimeSlot ?? app.AppointmentDate.ToString("HH:mm"),
                status = app.Status,
                subject = app.Subject,
                notes = app.Notes
            });
        }

        public IActionResult Settings()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SearchMedicines(string term)
        {
            var medicines = _medicineService.GetAllMedicines()
                .Where(m => m.MedicineName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(m => new { m.MedicineName, m.Strength, m.DosageForm })
                .ToList();
            return Json(medicines);
        }

        public IActionResult Medicines()
        {
            var medicines = _medicineService.GetAllMedicines();
            return View(medicines);
        }

        public IActionResult Templates()
        {
            return View();
        }
        public IActionResult AddPatient()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddPatient(Patient patient)
        {
            if (ModelState.IsValid)
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
                _patientService.AddPatient(patient);

                SendWelcomeEmail(patient.Email, patient.FirstName, randomPassword, "Patient");

                return RedirectToAction("Patient");
            }
            return View(patient);
        }

        public IActionResult AddStaff()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddStaff(string FirstName, string LastName, string Role, string Email, string Phone)
        {
            if (!string.IsNullOrEmpty(Email))
            {
                string randomPassword = PasswordGenerator.Generate();
                var newUser = new AppUser
                {
                    FirstName = FirstName,
                    LastName = LastName,
                    Email = Email,
                    Phone = Phone,
                    Password = randomPassword,
                    Role = "Staff",
                    Status = "Approved"
                };

                _userService.AddUser(newUser);

                SendWelcomeEmail(Email, FirstName, randomPassword, "Staff");

                return RedirectToAction("Staff");
            }
            return View();
        }

        private void SendWelcomeEmail(string email, string name, string password, string role)
        {
            string subject = $"MediCare Account Credentials - {role}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee;'>
                    <h2 style='color: #16a34a;'>Welcome to MediCare!</h2>
                    <p>Hello <strong>{name}</strong>,</p>
                    <p>An account has been created for you as a <strong>{role}</strong>.</p>
                    <div style='background: #f9f9f9; padding: 15px; margin: 20px 0;'>
                        <p><strong>Login Details:</strong></p>
                        <p>Email: {email}</p>
                        <p>Password: <span style='font-family: monospace; font-size: 1.2em;'>{password}</span></p>
                    </div>
                    <p>Please use these credentials to log in to your dashboard.</p>
                    <p>Regards,<br/>MediCare Smart OPD Team</p>
                </div>";

            _emailService.SendEmail(email, subject, body);
        }


        public IActionResult AddMedicine()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddMedicine(MediCare.Models.Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                medicine.GenericName ??= string.Empty;
                medicine.Category ??= string.Empty;
                medicine.DosageForm ??= string.Empty;
                medicine.Strength ??= string.Empty;
                medicine.PackSize ??= string.Empty;
                medicine.Manufacturer ??= string.Empty;
                medicine.Supplier ??= string.Empty;
                medicine.Unit ??= string.Empty;
                medicine.Storage ??= string.Empty;
                medicine.Usage ??= string.Empty;
                medicine.SideEffects ??= string.Empty;
                medicine.Instructions ??= string.Empty;
                medicine.PrescriptionRequired ??= string.Empty;

                _medicineService.AddMedicine(medicine);
                return RedirectToAction("Medicines");
            }
            return View(medicine);
        }

        public IActionResult EditMedicine(int id)
        {
            var medicine = _medicineService.GetMedicineById(id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [HttpPost]
        public IActionResult EditMedicine(MediCare.Models.Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                medicine.GenericName ??= string.Empty;
                medicine.Category ??= string.Empty;
                medicine.DosageForm ??= string.Empty;
                medicine.Strength ??= string.Empty;
                medicine.PackSize ??= string.Empty;
                medicine.Manufacturer ??= string.Empty;
                medicine.Supplier ??= string.Empty;
                medicine.Unit ??= string.Empty;
                medicine.Storage ??= string.Empty;
                medicine.Usage ??= string.Empty;
                medicine.SideEffects ??= string.Empty;
                medicine.Instructions ??= string.Empty;
                medicine.PrescriptionRequired ??= string.Empty;

                _medicineService.UpdateMedicine(medicine);
                LogAdminActivity("Medicine Updated", $"{medicine.MedicineName} updated.", "info");
                return RedirectToAction("Medicines");
            }
            return View(medicine);
        }

        [HttpPost]
        public IActionResult DeleteMedicine(int id)
        {
            _medicineService.DeleteMedicine(id);
            LogAdminActivity("Medicine Deleted", $"Medicine #{id} removed.", "error");
            return RedirectToAction("Medicines");
        }

        [HttpPost]
        public IActionResult DeleteSelectedMedicines([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });

            foreach (var id in ids)
            {
                _medicineService.DeleteMedicine(id);
            }

            LogAdminActivity("Medicines Deleted", $"{ids.Length} medicine record(s) removed.", "error");
            return Json(new { success = true });
        }
        public IActionResult EditDoctor(int id) 
        { 
            var doctor = _doctorService.GetDoctorById(id);
            if (doctor == null) return NotFound();
            return View(doctor); 
        }

        [HttpPost]
        public IActionResult EditDoctor(MediCare.Models.Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                _doctorService.UpdateDoctor(doctor);
                LogAdminActivity("Doctor Updated", $"{doctor.FirstName} {doctor.LastName} updated.", "info");
                return RedirectToAction("Doctors");
            }
            return View(doctor);
        }

        [HttpPost]
        public IActionResult DeleteDoctor(int id)
        {
            _doctorService.DeleteDoctor(id);
            LogAdminActivity("Doctor Deleted", $"Doctor #{id} removed.", "error");
            return RedirectToAction("Doctors");
        }
        public IActionResult EditPatient(int id) 
        { 
            var patient = _patientService.GetPatientById(id);
            if (patient == null) return NotFound();
            return View(patient); 
        }

        [HttpPost]
        public IActionResult EditPatient(MediCare.Models.Patient patient)
        {
            if (ModelState.IsValid)
            {
                _patientService.UpdatePatient(patient);
                LogAdminActivity("Patient Updated", $"{patient.FirstName} {patient.LastName} updated.", "info");
                return RedirectToAction("Patient"); // Assuming the list view is called Patient
            }
            return View(patient);
        }

        [HttpPost]
        public IActionResult DeletePatient(int id)
        {
            _patientService.DeletePatient(id);
            LogAdminActivity("Patient Deleted", $"Patient #{id} removed.", "error");
            return RedirectToAction("Patient");
        }
        public IActionResult EditOPDSchedule(int id) { return View(); }
        public IActionResult EditStaff(int id) { return View(); }
        public IActionResult EditTemplate(int id) { return View(); }

        public IActionResult PrescriptionDetails(int id) { return View(); }

        public IActionResult AddTemplate()
            {
                return View();
        }
        public IActionResult AddDoctor()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddDoctor(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                string randomPassword = PasswordGenerator.Generate();
                var newUser = new AppUser
                {
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    Email = doctor.Email,
                    Phone = doctor.Phone,
                    Password = randomPassword,
                    Role = "Doctor",
                    Status = "Approved"
                };

                _userService.AddUser(newUser);
                _doctorService.AddDoctor(doctor);

                SendWelcomeEmail(doctor.Email, doctor.FirstName, randomPassword, "Doctor");

                return RedirectToAction("Doctors");
            }
            return View(doctor);
        }

        public IActionResult PendingApprovals()
        {
            var pendingUsers = _userService.GetPendingUsers();
            return View(pendingUsers);
        }

        [HttpPost]
        public IActionResult ApproveUser(int id)
        {
            _userService.UpdateUserStatus(id, "Approved");
            LogAdminActivity("User Approved", $"User #{id} approved.", "success");
            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        public IActionResult RejectUser(int id)
        {
            _userService.UpdateUserStatus(id, "Rejected");
            LogAdminActivity("User Rejected", $"User #{id} rejected.", "warning");
            return RedirectToAction("PendingApprovals");
        }

        public IActionResult ManageUsers()
        {
            var allUsers = _userService.GetAllUsers();
            return View(allUsers);
        }

        [HttpPost]
        public IActionResult ChangeRole(int id, string role)
        {
            _userService.UpdateUserRole(id, role);
            LogAdminActivity("Role Changed", $"User #{id} role updated to {role}.", "info");
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public IActionResult ChangeStatus(int id, string status)
        {
            _userService.UpdateUserStatus(id, status);
            LogAdminActivity("Status Changed", $"User #{id} status updated to {status}.", "info");
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public IActionResult DeleteUser(int id, string returnUrl)
        {
            _userService.RemoveUser(id);
            LogAdminActivity("User Deleted", $"User #{id} removed.", "error");
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            await _feedbackService.DeleteFeedbackAsync(id);
            LogAdminActivity("Feedback Deleted", $"Feedback #{id} removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedFeedbacks([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            await _feedbackService.DeleteFeedbacksAsync(ids);
            LogAdminActivity("Feedbacks Deleted", $"{ids.Length} feedback item(s) removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllFeedbacks()
        {
            var feedbacks = await _feedbackService.GetAllFeedbacksAsync();
            await _feedbackService.DeleteFeedbacksAsync(feedbacks.Select(f => f.Id));
            LogAdminActivity("Feedbacks Cleared", "All feedback items removed.", "error");
            return Json(new { success = true });
        }

        public IActionResult Profile()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var user = _userService.GetUserByEmail(email) ?? new AppUser
            {
                FirstName = HttpContext.Session.GetString("UserName")?.Split(' ').FirstOrDefault() ?? "Admin",
                LastName = string.Join(" ", (HttpContext.Session.GetString("UserName") ?? "Admin").Split(' ').Skip(1)),
                Email = email,
                Phone = string.Empty,
                Role = HttpContext.Session.GetString("UserRole") ?? "Admin",
                Status = "Approved"
            };
            var pref = _userPreferenceService.GetOrCreate(email);
            ViewBag.PushNotificationsEnabled = pref.PushNotificationsEnabled;
            ViewBag.TwoFactorEnabled = pref.TwoFactorEnabled;
            ViewBag.ProfilePhotoUrl = GetAdminPhotoUrl(email);
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
                SaveAdminPhoto(email, photo);
            }

            _notificationService.AddNotification(new Notification
            {
                UserEmail = email,
                Title = "Profile Updated",
                Message = "Your admin profile was updated successfully.",
                Type = "success",
                CreatedAt = DateTime.UtcNow
            });
            TempData["ToastMessage"] = "Profile updated successfully";
            TempData["ToastType"] = "success";
            return RedirectToAction("Profile");
        }

        private string? GetAdminPhotoUrl(string email)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) return null;
            var prefix = GetAdminPhotoPrefix(email);
            var file = Directory.GetFiles(uploads, $"{prefix}.*").FirstOrDefault();
            return file == null ? null : $"/uploads/{Path.GetFileName(file)}";
        }

        private void SaveAdminPhoto(string email, IFormFile photo)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var prefix = GetAdminPhotoPrefix(email);
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

        private static string GetAdminPhotoPrefix(string email)
        {
            var safeEmail = new string((email ?? "admin").Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(safeEmail)) safeEmail = "admin";
            return $"admin_{safeEmail}";
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
            ViewBag.DeleteAllUrl = "/Admin/DeleteAllNotifications";
            ViewBag.DeleteUrl = "/Admin/DeleteNotifications";
            ViewBag.TitleText = "Activity History";
            ViewBag.SubtitleText = "All admin activities stored here";
            return View(list);
        }

        public IActionResult Notifications()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var list = string.IsNullOrEmpty(email) ? new List<Notification>() : _notificationService.GetNotificationsForUser(email);
            if (!string.IsNullOrEmpty(email)) _notificationService.MarkAllRead(email);
            return View(list);
        }

        [HttpPost]
        public IActionResult DeleteAllNotifications()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });
            _notificationService.DeleteAllForUser(email);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteNotifications([FromBody] int[] ids)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            if (string.IsNullOrEmpty(email) || ids == null) return Json(new { success = false });
            foreach (var id in ids) _notificationService.DeleteNotification(id, email);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult MarkAllNotificationsRead()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            if (string.IsNullOrEmpty(email)) return Json(new { success = false });
            _notificationService.MarkAllRead(email);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteAllPatients()
        {
            var patients = _patientService.GetAllPatients();
            foreach (var p in patients)
            {
                _patientService.DeletePatient(p.Id);
            }
            LogAdminActivity("Patients Cleared", "All patients removed.", "error");
            return RedirectToAction("Patient"); // The action is named 'Patient'
        }

        [HttpPost]
        public IActionResult DeleteAllDoctors()
        {
            var doctors = _doctorService.GetAllDoctors();
            foreach (var d in doctors)
            {
                _doctorService.DeleteDoctor(d.Id);
            }
            LogAdminActivity("Doctors Cleared", "All doctors removed.", "error");
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        public IActionResult DeleteAllMedicines()
        {
            var medicines = _medicineService.GetAllMedicines();
            foreach (var m in medicines)
            {
                _medicineService.DeleteMedicine(m.Id);
            }
            LogAdminActivity("Medicines Cleared", "All medicines removed.", "error");
            return RedirectToAction("Medicines");
        }

        [HttpPost]
        public IActionResult DeleteAllStaff()
        {
            var staff = _userService.GetStaffUsers();
            foreach (var s in staff)
            {
                _userService.RemoveUser(s.Id);
            }
            LogAdminActivity("Staff Cleared", "All staff users removed.", "error");
            return RedirectToAction("Staff");
        }
    }
}
