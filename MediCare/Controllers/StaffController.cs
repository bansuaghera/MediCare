using Microsoft.AspNetCore.Mvc;
using MediCare.Services;
using MediCare.Models;
using MediCare.Utilities;

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

        public StaffController(PatientService patientService, AppointmentService appointmentService, UserService userService, EmailService emailService, DoctorService doctorService, QueueService queueService)
        {
            _patientService = patientService;
            _appointmentService = appointmentService;
            _userService = userService;
            _emailService = emailService;
            _doctorService = doctorService;
            _queueService = queueService;
        }

        public IActionResult Dashboard()
        {
            var today = DateTime.Today;
            var appointments = _appointmentService.GetAllAppointments();

            ViewBag.TodayCount = appointments.Count(a => a.AppointmentDate.Date == today);
            ViewBag.WaitingCount = appointments.Count(a => a.AppointmentDate.Date == today && (a.Status == "Waiting" || a.Status == "Scheduled"));
            ViewBag.InProgressCount = appointments.Count(a => a.AppointmentDate.Date == today && a.Status == "In Progress");
            ViewBag.CompletedCount = appointments.Count(a => a.AppointmentDate.Date == today && a.Status == "Completed");
            ViewBag.TotalPatients = _patientService.GetAllPatients().Count;
            ViewBag.TotalDoctors = _doctorService.GetAllDoctors().Count;

            ViewBag.RecentAppointments = appointments
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .ToList();

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
            return View();
        }
        public IActionResult CallToken()
        {
            var todayAppts = _appointmentService.GetAllAppointments()
                .Where(a => a.AppointmentDate.Date == DateTime.Today && (a.Status == "Scheduled" || a.Status == "In-Progress"))
                .OrderBy(a => a.AppointmentDate)
                .ToList();
            
            ViewBag.CurrentTokens = _queueService.GetAllCurrentTokens();
            return View(todayAppts);
        }

        [HttpPost]
        public IActionResult ServeToken(int doctorId, string tokenNumber)
        {
            _queueService.SetCurrentToken(doctorId, tokenNumber);
            return Json(new { success = true });
        }
        public IActionResult Schedule()
        {
            return View();
        }
        public IActionResult Appointments()
        {
            var appointments = _appointmentService.GetAllAppointments()
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.TimeSlot)
                .ToList();
            
            ViewBag.Doctors = _doctorService.GetAllDoctors();
            ViewBag.Patients = _patientService.GetAllPatients();
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
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteAppointment(int id)
        {
            _appointmentService.DeleteAppointment(id);
            return Json(new { success = true });
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
            var appointments = _appointmentService.GetAllAppointments();
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
                
                // Generate token number manually
                var countToday = _appointmentService.GetAllAppointments().Count(a => a.AppointmentDate.Date == DateTime.Today);
                appointment.TokenNumber = (countToday + 1).ToString("D3");
                
                _appointmentService.AddAppointment(appointment);
                return RedirectToAction("TodayAppointments");
            }
            ViewBag.Doctors = _doctorService.GetAllDoctors();
            ViewBag.Patients = _patientService.GetAllPatients();
            return View(appointment);
        }
        public IActionResult PrintAppointment(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment == null) return NotFound();
            return View(appointment);
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }
    }
}
