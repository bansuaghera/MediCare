using Microsoft.AspNetCore.Mvc;
using MediCare.Models;
using MediCare.Services;

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

        public UserController(AppointmentService appointmentService, PatientService patientService, DoctorService doctorService, UserService userService, PrescriptionService prescriptionService, FeedbackService feedbackService)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
            _doctorService = doctorService;
            _userService = userService;
            _prescriptionService = prescriptionService;
            _feedbackService = feedbackService;
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

            return View(appointments);
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
                
                // Auto-generate a Token Number if not provided
                if (string.IsNullOrEmpty(appointment.TokenNumber))
                {
                    int totalToday = _appointmentService.GetAllAppointments()
                        .Count(a => a.DoctorId == appointment.DoctorId && a.AppointmentDate.Date == appointment.AppointmentDate.Date);
                    appointment.TokenNumber = "TN-" + (100 + totalToday + 1);
                }

                // Ensure the date is UTC for PostgreSQL
                appointment.AppointmentDate = DateTime.SpecifyKind(appointment.AppointmentDate, DateTimeKind.Utc);
                
                _appointmentService.AddAppointment(appointment);
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

            MediCare.Models.Appointment? appointment = null;
            if (patient != null)
            {
                // Get the most recent upcoming appointment for today
                appointment = _appointmentService.GetAllAppointments()
                    .Where(a => a.PatientId == patient.Id && a.AppointmentDate.Date == DateTime.Today && (a.Status == "Waiting" || a.Status == "Scheduled" || a.Status == "In Progress"))
                    .OrderBy(a => a.AppointmentDate)
                    .FirstOrDefault();
                
                if (appointment != null && appointment.Status == "Waiting")
                {
                    // Count patients ahead of this one for the same doctor today
                    var ahead = _appointmentService.GetAllAppointments()
                        .Count(a => a.DoctorId == appointment.DoctorId && a.AppointmentDate.Date == DateTime.Today && a.Status == "Waiting" && a.Id < appointment.Id);
                    ViewBag.PatientsAhead = ahead;
                }
            }

            return View(appointment);
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
            return View(patient);
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
        public async Task<IActionResult> SubmitFeedback(Feedback feedback)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var patient = _patientService.GetAllPatients().FirstOrDefault(p => p.Email != null && p.Email.ToLower() == email.ToLower());

            feedback.SubmittedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
            if (patient != null)
            {
                feedback.PatientId = patient.Id;
            }
            
            await _feedbackService.AddFeedbackAsync(feedback);
            return Json(new { success = true });
        }

        public IActionResult Notifications()
        {
            return View();
        }

        public IActionResult Feedback()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}
