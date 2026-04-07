using Microsoft.AspNetCore.Mvc;
using MediCare.Services;
using MediCare.Models;

namespace MediCare.Controllers
{
    public class DoctorController : Controller
    {
        private readonly AppointmentService _appointmentService;
        private readonly PatientService _patientService;
        private readonly PrescriptionService _prescriptionService;
        private readonly DoctorService _doctorService;
        private readonly QueueService _queueService;
        private readonly PrescriptionTemplateService _templateService;

        public DoctorController(
            AppointmentService appointmentService, 
            PatientService patientService,
            PrescriptionService prescriptionService,
            DoctorService doctorService,
            QueueService queueService,
            PrescriptionTemplateService templateService)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
            _prescriptionService = prescriptionService;
            _doctorService = doctorService;
            _queueService = queueService;
            _templateService = templateService;
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
            return View(model);
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
            return RedirectToAction("Appointments");
        }

        public IActionResult Patients()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            if (doctor == null) return RedirectToAction("Login", "Login");

            var patients = _appointmentService.GetAllAppointments()
                .Where(a => a.DoctorId == doctor.Id)
                .Select(a => a.Patient)
                .Distinct()
                .ToList();
            
            return View(patients);
        }

        [HttpGet]
        public async Task<IActionResult> Examination(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment == null) return NotFound();

            var prescription = new Models.Prescription
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                DateIssued = DateTime.Now
            };

            ViewBag.PatientName = (appointment.Patient?.FirstName ?? "Unknown") + " " + (appointment.Patient?.LastName ?? "Patient");
            ViewBag.Templates = await _templateService.GetAllTemplatesAsync();
            return View(prescription);
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplateDetails(int id)
        {
            var template = await _templateService.GetTemplateByIdAsync(id);
            if(template == null) return NotFound();
            return Json(new { diagnosis = template.Diagnosis, additionalNotes = template.AdditionalNotes, medicines = template.MedicineNotes });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> Examination(Models.Prescription prescription)
        {
            prescription.DateIssued = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            await _prescriptionService.CreatePrescriptionAsync(prescription);

            var appointment = _appointmentService.GetAppointmentById(prescription.AppointmentId);
            if (appointment != null)
            {
                appointment.Status = "Completed";
                _appointmentService.UpdateAppointment(appointment);
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
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult ToggleEmergency(int id)
        {
            var app = _appointmentService.GetAppointmentById(id);
            if (app != null)
            {
                app.IsEmergency = !app.IsEmergency;
                _appointmentService.UpdateAppointment(app);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public IActionResult Profile()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "";
            var doctor = _doctorService.GetDoctorByEmail(email);
            return View(doctor);
        }

        public async Task<IActionResult> PrintPrescription(int id)
        {
            var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
            if (prescription == null) return NotFound();
            return View(prescription);
        }

        [HttpPost]
        public IActionResult BulkDeleteAppointments([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Json(new { success = false });
            foreach (var id in ids)
            {
                _appointmentService.DeleteAppointment(id);
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
                _appointmentService.DeleteAppointment(app.Id);
            }
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
