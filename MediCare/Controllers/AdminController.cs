using Microsoft.AspNetCore.Mvc;
using MediCare.Services;
using MediCare.Models;
using MediCare.Utilities;

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

        public AdminController(UserService userService, PatientService patientService, DoctorService doctorService, MedicineService medicineService, AppointmentService appointmentService, EmailService emailService, PrescriptionService prescriptionService, QueueService queueService, FeedbackService feedbackService, OPDScheduleService opdScheduleService)
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
        }
        public IActionResult Dashboard()
        {
            var appointments = _appointmentService.GetAllAppointments();
            var model = new MediCare.Models.ViewModels.AdminDashboardViewModel
            {
                TotalPatients = _patientService.GetAllPatients().Count,
                ActiveDoctors = _doctorService.GetAllDoctors().Count,
                AppointmentsToday = appointments.Count(a => a.AppointmentDate.Date == DateTime.Today),
                TotalRevenue = _doctorService.GetAllDoctors().Sum(d => d.ConsultationFee),
                RecentAppointments = appointments.OrderByDescending(a => a.AppointmentDate).Take(5).ToList(),
                
                // Mocking weekly chart data for demo functionality
                ChartLabels = new List<string> { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                WeeklyPatients = new List<int> { 12, 19, 15, 25, 22, 10, 5 },
                WeeklyAppointments = new List<int> { 15, 22, 18, 30, 25, 12, 8 }
            };
            return View(model);
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
                .Where(a => a.AppointmentDate.Date == DateTime.Today && a.Status == "Scheduled")
                .OrderBy(a => a.AppointmentDate)
                .ToList();

            ViewBag.CurrentTokens = _queueService.GetAllCurrentTokens();
            return View(appointments);
        }

        [HttpPost]
        public IActionResult ServeToken(int doctorId, string tokenNumber)
        {
            _queueService.SetCurrentToken(doctorId, tokenNumber);
            return Json(new { success = true });
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
                _medicineService.UpdateMedicine(medicine);
                return RedirectToAction("Medicines");
            }
            return View(medicine);
        }

        [HttpPost]
        public IActionResult DeleteMedicine(int id)
        {
            _medicineService.DeleteMedicine(id);
            return RedirectToAction("Medicines");
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
                return RedirectToAction("Doctors");
            }
            return View(doctor);
        }

        [HttpPost]
        public IActionResult DeleteDoctor(int id)
        {
            _doctorService.DeleteDoctor(id);
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
                return RedirectToAction("Patient"); // Assuming the list view is called Patient
            }
            return View(patient);
        }

        [HttpPost]
        public IActionResult DeletePatient(int id)
        {
            _patientService.DeletePatient(id);
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
            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        public IActionResult RejectUser(int id)
        {
            _userService.UpdateUserStatus(id, "Rejected");
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
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public IActionResult ChangeStatus(int id, string status)
        {
            _userService.UpdateUserStatus(id, status);
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        public IActionResult DeleteUser(int id, string returnUrl)
        {
            _userService.RemoveUser(id);
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("ManageUsers");
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DeleteAllAppointments()
        {
            var appointments = _appointmentService.GetAllAppointments();
            foreach (var app in appointments)
            {
                _appointmentService.DeleteAppointment(app.Id);
            }
            return RedirectToAction("Appointments");
        }

        [HttpPost]
        public IActionResult DeleteAllPatients()
        {
            var patients = _patientService.GetAllPatients();
            foreach (var p in patients)
            {
                _patientService.DeletePatient(p.Id);
            }
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
            return RedirectToAction("Staff");
        }
    }
}
