using Microsoft.AspNetCore.Mvc;
using MediCare.Services;
using MediCare.Models;
using MediCare.Utilities;
using System.Text.Json;
using System.IO;

namespace MediCare.Controllers
{
    public class DoctorController : Controller
    {
        private readonly AppointmentService _appointmentService;
        private readonly PatientService _patientService;
        private readonly PrescriptionService _prescriptionService;
        private readonly DoctorService _doctorService;
        private readonly MedicineService _medicineService;
        private readonly QueueService _queueService;
        private readonly PrescriptionTemplateService _templateService;
        private readonly ClinicBranchService _branchService;
        private readonly NotificationService _notificationService;
        private readonly UserPreferenceService _userPreferenceService;
        private readonly FeedbackService _feedbackService;

        public DoctorController(
            AppointmentService appointmentService, 
            PatientService patientService,
            PrescriptionService prescriptionService,
            DoctorService doctorService,
            MedicineService medicineService,
            QueueService queueService,
            PrescriptionTemplateService templateService,
            ClinicBranchService branchService,
            NotificationService notificationService,
            UserPreferenceService userPreferenceService,
            FeedbackService feedbackService)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
            _prescriptionService = prescriptionService;
            _doctorService = doctorService;
            _medicineService = medicineService;
            _queueService = queueService;
            _templateService = templateService;
            _branchService = branchService;
            _notificationService = notificationService;
            _userPreferenceService = userPreferenceService;
            _feedbackService = feedbackService;
        }

        private void LogDoctorActivity(string title, string message, string type = "info")
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
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            
            if (doctor == null)
            {
                return RedirectToAction("Login", "Login");
            }

            var allAppointments = _appointmentService.GetAllAppointments()
                .Where(a => a.DoctorId == doctor.Id)
                .ToList();
            var statusCounts = AnalyticsHelper.BuildStatusCounts(allAppointments);
            var dailyTrend = AnalyticsHelper.BuildDailyAppointmentTrend(allAppointments, 7);

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var model = new MediCare.Models.ViewModels.DoctorDashboardViewModel
            {
                TodayAppointmentsCount = allAppointments.Count(a => a.AppointmentDate.Date == today),
                WaitingPatientsCount = allAppointments.Count(a => a.AppointmentDate.Date == today && (a.Status == "Waiting" || a.Status == "Scheduled")),
                CompletedAppointmentsCount = allAppointments.Count(a => a.AppointmentDate.Date == today && a.Status == "Completed"),
                TomorrowAppointmentsCount = allAppointments.Count(a => a.AppointmentDate.Date == tomorrow),
                TodayAppointments = allAppointments.Where(a => a.AppointmentDate.Date == today).OrderBy(a => a.AppointmentDate).ToList(),
                TotalPatientsCount = allAppointments.Select(a => a.PatientId).Distinct().Count(),
                PrescriptionsIssuedCount = allAppointments.Count(a => a.Status == "Completed"),
                TodayProgressPercentage = allAppointments.Count(a => a.AppointmentDate.Date == today) > 0 
                    ? (double)allAppointments.Count(a => a.AppointmentDate.Date == today && a.Status == "Completed") / allAppointments.Count(a => a.AppointmentDate.Date == today) * 100 
                    : 0
            };

            ViewBag.CurrentToken = _queueService.GetCurrentToken(doctor.Id);
            ViewBag.ChartLabels = statusCounts.Labels;
            ViewBag.ChartData = statusCounts.Counts;
            ViewBag.DailyTrendLabels = dailyTrend.Labels;
            ViewBag.DailyTrendData = dailyTrend.Counts;
            return View(model);
        }

        public IActionResult Medicines()
        {
            var medicines = _medicineService.GetAllMedicines();
            return View(medicines);
        }

        [HttpPost]
        public IActionResult AddMedicine(Medicine medicine)
        {
            if (!ModelState.IsValid) return RedirectToAction("Medicines");

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
            LogDoctorActivity("Medicine Added", $"{medicine.MedicineName} added to inventory.", "success");
            return RedirectToAction("Medicines");
        }

        [HttpPost]
        public IActionResult DeleteMedicine(int id)
        {
            _medicineService.DeleteMedicine(id);
            LogDoctorActivity("Medicine Deleted", $"Medicine #{id} removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteSelectedMedicines([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            foreach (var id in ids) _medicineService.DeleteMedicine(id);
            LogDoctorActivity("Medicines Deleted", $"{ids.Length} medicine record(s) removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteAllMedicines()
        {
            foreach (var med in _medicineService.GetAllMedicines())
            {
                _medicineService.DeleteMedicine(med.Id);
            }
            LogDoctorActivity("Medicines Cleared", "All medicines removed.", "error");
            return Json(new { success = true });
        }

        public async Task<IActionResult> Templates()
        {
            ViewBag.Templates = await _templateService.GetAllTemplatesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            await _templateService.DeleteTemplateAsync(id);
            LogDoctorActivity("Template Deleted", $"Template #{id} removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedTemplates([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            foreach (var id in ids) await _templateService.DeleteTemplateAsync(id);
            LogDoctorActivity("Templates Deleted", $"{ids.Length} template record(s) removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllTemplates()
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            foreach (var t in templates) await _templateService.DeleteTemplateAsync(t.Id);
            LogDoctorActivity("Templates Cleared", "All quick templates removed.", "error");
            return Json(new { success = true });
        }

        public async Task<IActionResult> Branches()
        {
            ViewBag.Branches = await _templateService.GetAllBranchesAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            await _templateService.DeleteTemplateAsync(id);
            LogDoctorActivity("Branch Deleted", $"Branch #{id} removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedBranches([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            foreach (var id in ids) await _templateService.DeleteTemplateAsync(id);
            LogDoctorActivity("Branches Deleted", $"{ids.Length} branch record(s) removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllBranches()
        {
            var branches = await _templateService.GetAllBranchesAsync();
            foreach (var b in branches) await _templateService.DeleteTemplateAsync(b.Id);
            LogDoctorActivity("Branches Cleared", "All branches removed.", "error");
            return Json(new { success = true });
        }

        public IActionResult Analytics()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            var appointments = _appointmentService.GetAllAppointments().Where(a => a.DoctorId == doctor.Id).ToList();
            var statusCounts = AnalyticsHelper.BuildStatusCounts(appointments);
            var dailyTrend = AnalyticsHelper.BuildDailyAppointmentTrend(appointments, 14);

            ViewBag.TotalPatients = appointments.Select(a => a.PatientId).Distinct().Count();
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.CompletedCount = appointments.Count(a => a.Status == "Completed");
            ViewBag.WaitingCount = appointments.Count(a => a.Status == "Waiting" || a.Status == "Scheduled");
            ViewBag.ChartLabels = statusCounts.Labels;
            ViewBag.ChartData = statusCounts.Counts;
            ViewBag.DailyTrendLabels = dailyTrend.Labels;
            ViewBag.DailyTrendData = dailyTrend.Counts;
            ViewBag.RecentAppointments = appointments.OrderByDescending(a => a.AppointmentDate).Take(8).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult ServeNext(int appointmentId)
        {
            var appointment = _appointmentService.GetAppointmentById(appointmentId);
            if (appointment != null)
            {
                appointment.Status = "In Progress";
                _appointmentService.UpdateAppointment(appointment);
                if (appointment.TokenNumber != null)
                {
                    _queueService.SetCurrentToken(appointment.DoctorId, appointment.TokenNumber);
                }
                if (appointment.Patient?.Email != null)
                {
                    _notificationService.AddNotification(new Notification
                    {
                        UserEmail = appointment.Patient.Email,
                        Title = "Appointment Called In",
                        Message = $"Token {appointment.TokenNumber} is now In Progress",
                        Type = "info",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                LogDoctorActivity("Token Called In", $"Token {appointment.TokenNumber} moved to In Progress.", "info");
                return Json(new { success = true, token = appointment.TokenNumber });
            }
            return Json(new { success = false });
        }
        
        public IActionResult Appointments()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            
            var appointments = _appointmentService.GetAllAppointments();
            
            if (doctor != null)
            {
                var results = appointments.Where(a => a.DoctorId == doctor.Id && 
                    (a.AppointmentDate.Date >= DateTime.Today || a.Status == "Waiting" || a.Status == "In Progress"))
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.TimeSlot)
                    .ToList();
                
                ViewBag.WaitingCount = results.Count(a => a.Status != "Completed");
                ViewBag.DoneCount = results.Count(a => a.Status == "Completed");
                appointments = results;
            }
            
            ViewBag.Patients = _patientService.GetAllPatients();
            return View(appointments);
        }

        [HttpPost]
        public IActionResult AddEmergencyAppointment(Appointment appointment)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            appointment.DoctorId = doctor.Id;
            appointment.TimeSlot = DateTime.Now.ToString("hh:mm tt");
            appointment.Status = "Waiting";
            appointment.IsEmergency = true;
            appointment.TokenNumber = "EMG-" + DateTime.Now.ToString("ss"); 

            _appointmentService.AddAppointment(appointment);
            LogDoctorActivity("Emergency Appointment Added", $"Emergency appointment for {appointment.Subject ?? "patient"} created.", "warning");
            return RedirectToAction("Appointments");
        }

        public IActionResult Patients()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            var doctorAppointments = _appointmentService.GetAllAppointments()
                .Where(a => a.DoctorId == doctor.Id)
                .ToList();

            var patients = doctorAppointments
                .Select(a => a.Patient)
                .Distinct()
                .ToList();

            // Map each patient to the most recent appointment date for quick filtering in the UI
            ViewBag.PatientLastVisit = doctorAppointments
                .GroupBy(a => a.PatientId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.AppointmentDate));
            
            return View(patients);
        }

        [HttpPost]
        public IActionResult DeletePatient(int id)
        {
            var patient = _patientService.GetPatientById(id);
            if (patient != null)
            {
                _patientService.DeletePatient(id);
                LogDoctorActivity("Patient Deleted", $"{patient.FirstName} {patient.LastName} removed from doctor panel.", "error");
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult BulkDeletePatients([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });

            foreach (var id in ids)
            {
                _patientService.DeletePatient(id);
            }

            LogDoctorActivity("Patients Deleted", $"{ids.Length} patient record(s) deleted.", "error");

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> Examination(int? id)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            var allDoctorApps = _appointmentService.GetAllAppointments()
                .Where(a => a.DoctorId == doctor.Id)
                .ToList();

            var appointmentOptions = allDoctorApps
                .OrderByDescending(a => a.Status == "In Progress" || a.Status == "Waiting" || a.Status == "Scheduled")
                .ThenByDescending(a => a.IsEmergency)
                .ThenBy(a => a.SortOrder)
                .ThenBy(a => a.AppointmentDate)
                .ToList();

            var appointment = id.HasValue
                ? allDoctorApps.FirstOrDefault(a => a.Id == id.Value)
                : appointmentOptions.FirstOrDefault();

            if (appointment == null)
            {
                TempData["Error"] = "No appointment found for examination.";
                ViewBag.ActiveAppointments = new List<Appointment>();
                ViewBag.Templates = await _templateService.GetAllTemplatesAsync();
                ViewBag.Branches = await _templateService.GetAllBranchesAsync();
                ViewBag.PatientName = "No appointment selected";
                return View(new Models.Prescription());
            }

            // Ensure the selected appointment appears in the dropdown even if it is not currently active
            if (!appointmentOptions.Any(a => a.Id == appointment.Id))
            {
                appointmentOptions.Insert(0, appointment);
            }

            var prescription = new Models.Prescription
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                DateIssued = DateTime.Now
            };

            ViewBag.PatientName = (appointment.Patient?.FirstName ?? "Unknown") + " " + (appointment.Patient?.LastName ?? "Patient");
            ViewBag.Templates = await _templateService.GetAllTemplatesAsync();
            ViewBag.Branches = await _templateService.GetAllBranchesAsync();
            ViewBag.Medicines = _medicineService.GetAllMedicines();
            ViewBag.ActiveAppointments = appointmentOptions;
            ViewBag.SelectedAppointmentId = appointment.Id;
            return View(prescription);
        }

        [HttpGet]
        public IActionResult SearchMedicines(string term)
        {
            var medicines = _medicineService.GetAllMedicines()
                .Where(m => !string.IsNullOrWhiteSpace(m.MedicineName) && m.MedicineName.Contains(term ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .Take(12)
                .Select(m => new
                {
                    m.Id,
                    m.MedicineName,
                    m.Strength,
                    m.DosageForm,
                    m.Category,
                    m.Manufacturer
                })
                .ToList();
            return Json(medicines);
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplateDetails(int id)
        {
            var template = await _templateService.GetTemplateByIdAsync(id);
            if(template == null) return NotFound();
            return Json(new { diagnosis = template.Diagnosis, additionalNotes = template.AdditionalNotes, medicines = template.MedicineNotes });
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplates()
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            return Json(templates.Select(t => new { id = t.Id, title = t.Title }));
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _templateService.GetAllBranchesAsync();
            return Json(branches.Select(b => new { id = b.Id, name = b.Title, location = b.AdditionalNotes }));
        }

        [HttpPost]
        public async Task<IActionResult> AddQuickTemplate(string title, string diagnosis, string medicineNotes, string? additionalNotes)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(diagnosis))
            {
                return Json(new { success = false });
            }

            var template = new PrescriptionTemplate
            {
                Title = title.Trim(),
                Diagnosis = diagnosis.Trim(),
                MedicineNotes = medicineNotes ?? string.Empty,
                AdditionalNotes = additionalNotes
            };

            await _templateService.AddTemplateAsync(template);

            return Json(new { success = true, id = template.Id, title = template.Title });
        }

        [HttpPost]
        public async Task<IActionResult> AddBranch(string name, string? location)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false });
            }

            var branch = new PrescriptionTemplate
            {
                Title = name.Trim(),
                Diagnosis = string.Empty,
                MedicineNotes = string.Empty,
                AdditionalNotes = location?.Trim()
            };

            await _templateService.AddBranchAsync(branch);

            return Json(new { success = true, id = branch.Id, name = branch.Title, location = branch.AdditionalNotes });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> Examination(Models.Prescription prescription)
        {
            // Ensure doctor/patient linkage from appointment for safety
            var appointment = _appointmentService.GetAppointmentById(prescription.AppointmentId);
            if (appointment == null) return NotFound();
            prescription.PatientId = appointment.PatientId;
            prescription.DoctorId = appointment.DoctorId;
            prescription.DateIssued = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            await _prescriptionService.CreatePrescriptionAsync(prescription);

            appointment.Status = "Completed";
            _appointmentService.UpdateAppointment(appointment);
            _queueService.ClearCurrentToken(appointment.DoctorId);

            LogDoctorActivity("Consultation Completed", $"Appointment #{appointment.Id} marked completed and prescription saved.", "success");
            if (appointment.Patient?.Email != null)
            {
                _notificationService.AddNotification(new Notification
                {
                    UserEmail = appointment.Patient.Email,
                    Title = "Appointment Completed",
                    Message = $"Your consultation for token {appointment.TokenNumber} is completed.",
                    Type = "success",
                    CreatedAt = DateTime.UtcNow
                });
            }

            return RedirectToAction("PrintPrescription", new { id = prescription.Id });
        }

        public async System.Threading.Tasks.Task<IActionResult> Prescriptions()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            var prescriptions = await _prescriptionService.GetAllPrescriptionsAsync();
            prescriptions = prescriptions.Where(p => p.DoctorId == doctor.Id).ToList();
            return View(prescriptions);
        }

        [HttpGet]
        public async Task<IActionResult> Feedback()
        {
            string name = HttpContext.Session.GetString("UserName") ?? "Doctor";
            var history = await _feedbackService.GetFeedbacksByNameAsync(name);
            return View(history);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(Feedback feedback, string? subject)
        {
            string name = HttpContext.Session.GetString("UserName") ?? "Doctor";
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
            string name = HttpContext.Session.GetString("UserName") ?? "Doctor";
            var fb = await _feedbackService.GetFeedbackByIdAsync(id);
            if (fb == null || !string.Equals(fb.PatientName, name, StringComparison.OrdinalIgnoreCase)) return Json(new { success = false });
            await _feedbackService.DeleteFeedbackAsync(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelectedFeedbacks([FromBody] int[] ids)
        {
            string name = HttpContext.Session.GetString("UserName") ?? "Doctor";
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
            string name = HttpContext.Session.GetString("UserName") ?? "Doctor";
            await _feedbackService.DeleteAllForNameAsync(name);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePrescription(int id)
        {
            await _prescriptionService.DeletePrescriptionAsync(id);
            LogDoctorActivity("Prescription Deleted", $"Prescription #{id} removed.", "error");
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> BulkDeletePrescriptions([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            foreach (var id in ids)
            {
                await _prescriptionService.DeletePrescriptionAsync(id);
            }
            LogDoctorActivity("Prescriptions Deleted", $"{ids.Length} prescription(s) removed.", "error");
            return Json(new { success = true });
        }

        public IActionResult Schedule()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            var appointments = _appointmentService.GetAllAppointments()
                .Where(a => a.DoctorId == doctor.Id)
                .OrderBy(a => a.AppointmentDate)
                .ToList();
            return View(appointments);
        }

        public IActionResult FollowUps()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            var followUps = _appointmentService.GetAllAppointments()
                .Where(a => a.DoctorId == doctor.Id && (a.Subject != null && a.Subject.Contains("Follow-up", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            return View(followUps);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment != null)
            {
                appointment.Status = status;
                _appointmentService.UpdateAppointment(appointment); 
                if (string.Equals(appointment.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    _queueService.ClearCurrentToken(appointment.DoctorId);
                }
                if (appointment.Patient?.Email != null)
                {
                    _notificationService.AddNotification(new Notification
                    {
                        UserEmail = appointment.Patient.Email,
                        Title = "Appointment Status Updated",
                        Message = $"Token {appointment.TokenNumber} status -> {status}",
                        Type = "info",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                LogDoctorActivity("Appointment Status Updated", $"Appointment #{id} status changed to {status}.", "info");
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // ---------------- TOKEN QUEUE (Doctor) ----------------

        [HttpGet]
        public IActionResult TokenQueue()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            var appointments = _appointmentService.GetAllAppointments()
                .Where(a => a.DoctorId == doctor.Id)
                .OrderByDescending(a => a.IsEmergency)
                .ThenBy(a => a.SortOrder)
                .ThenBy(a => a.AppointmentDate)
                .ToList();

            ViewBag.WaitingCount = appointments.Count(a => a.Status == "Waiting" || a.Status == "Scheduled");
            ViewBag.InProgressCount = appointments.Count(a => a.Status == "In Progress" || a.Status == "In-Progress");
            ViewBag.CompletedCount = appointments.Count(a => a.Status == "Completed");
            ViewBag.TotalToday = appointments.Count;

            return View(appointments);
        }

        [HttpPost]
        public IActionResult UpdateOrder([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            _appointmentService.UpdateSortOrder(ids);
            LogDoctorActivity("Queue Reordered", $"{ids.Length} item(s) reordered in the queue.", "info");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ToggleEmergency(int id)
        {
            var app = _appointmentService.GetAppointmentById(id);
            if (app != null)
            {
                app.IsEmergency = !app.IsEmergency;
                _appointmentService.UpdateAppointment(app);
                LogDoctorActivity("Emergency Flag Updated", $"Appointment #{id} emergency set to {app.IsEmergency}.", "warning");
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public IActionResult Profile()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            var pref = _userPreferenceService.GetOrCreate(email);
            ViewBag.PushNotificationsEnabled = pref.PushNotificationsEnabled;
            ViewBag.TwoFactorEnabled = pref.TwoFactorEnabled;
            if (doctor != null)
            {
                ViewBag.ProfilePhotoUrl = GetDoctorPhotoUrl(doctor);
            }
            return View(doctor);
        }

        [HttpPost]
        public IActionResult Profile(Doctor form, IFormFile? photo)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            doctor.FirstName = form.FirstName;
            doctor.LastName = form.LastName;
            doctor.Specialty = form.Specialty;
            doctor.LicenseNumber = form.LicenseNumber;
            doctor.Phone = form.Phone;
            doctor.ExperienceYears = form.ExperienceYears;
            doctor.ConsultationFee = form.ConsultationFee;

            if (photo != null && photo.Length > 0)
            {
                SaveDoctorPhoto(doctor, photo);
            }

            _doctorService.UpdateDoctor(doctor);
            _notificationService.AddNotification(new Notification
            {
                UserEmail = email,
                Title = "Profile Updated",
                Message = "Your doctor profile was updated successfully.",
                Type = "success",
                CreatedAt = DateTime.UtcNow
            });
            TempData["ToastMessage"] = "Profile updated successfully";
            TempData["ToastType"] = "success";
            return RedirectToAction("Profile");
        }

        private string? GetDoctorPhotoUrl(Doctor doctor)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) return null;

            var prefix = GetDoctorPhotoPrefix(doctor);
            var file = Directory.GetFiles(uploads, $"{prefix}.*").FirstOrDefault();
            return file == null ? null : $"/uploads/{Path.GetFileName(file)}";
        }

        private void SaveDoctorPhoto(Doctor doctor, IFormFile photo)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var prefix = GetDoctorPhotoPrefix(doctor);
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

        private static string GetDoctorPhotoPrefix(Doctor doctor)
        {
            var safeEmail = new string((doctor.Email ?? "doctor").Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(safeEmail)) safeEmail = "doctor";
            return $"doctor_{doctor.Id}_{safeEmail}";
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

        public async Task<IActionResult> PrintPrescription(int id)
        {
            var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
            if (prescription == null) return NotFound();
            return View(prescription);
        }

        [HttpGet]
        public async Task<IActionResult> GetPrescriptionDetails(int id)
        {
            var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
            if (prescription == null) return NotFound();

            return Json(new
            {
                id = prescription.Id,
                patientName = $"{prescription.Patient?.FirstName} {prescription.Patient?.LastName}".Trim(),
                doctorName = $"Dr. {prescription.Doctor?.FirstName} {prescription.Doctor?.LastName}".Trim(),
                patientId = prescription.PatientId,
                token = prescription.Appointment?.TokenNumber,
                dateIssued = prescription.DateIssued.ToString("yyyy-MM-dd HH:mm"),
                diagnosis = prescription.Diagnosis,
                medicineNotes = prescription.MedicineNotes,
                additionalNotes = prescription.AdditionalNotes
            });
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
        public IActionResult BulkDeleteAppointments([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            foreach (var id in ids)
            {
                var app = _appointmentService.GetAppointmentById(id);
                if (app != null)
                {
                    _queueService.ClearCurrentToken(app.DoctorId);
                    _appointmentService.DeleteAppointment(id);
                }
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteAllAppointments()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return Json(new { success = false });

            var appointments = _appointmentService.GetAllAppointments()
                .Where(a => a.DoctorId == doctor.Id)
                .ToList();

            foreach (var app in appointments)
            {
                _queueService.ClearCurrentToken(app.DoctorId);
                _appointmentService.DeleteAppointment(app.Id);
            }
            LogDoctorActivity("Appointments Cleared", "All doctor appointments were removed.", "error");
            return Json(new { success = true });
        }

        public IActionResult History()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Login");

            var list = _notificationService.GetAllNotificationsForUser(email);
            ViewBag.DeleteAllUrl = "/Doctor/DeleteAllNotifications";
            ViewBag.DeleteUrl = "/Doctor/DeleteNotifications";
            ViewBag.TitleText = "Activity History";
            ViewBag.SubtitleText = "All doctor activities stored here";
            return View(list);
        }

        [HttpGet]
        public IActionResult GetAppointmentDetails(int id)
        {
            var app = _appointmentService.GetAppointmentById(id);
            if (app == null) return NotFound();

            return Json(new {
                id = app.Id,
                patientName = app.Patient?.FirstName + " " + app.Patient?.LastName,
                email = app.Patient?.Email,
                phone = app.Patient?.Phone,
                date = app.AppointmentDate.ToString("yyyy-MM-dd"),
                time = app.TimeSlot,
                token = app.TokenNumber,
                status = app.Status,
                subject = app.Subject,
                notes = app.Notes
            });
        }
    }
}
