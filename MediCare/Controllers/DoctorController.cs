using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class DoctorController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        public IActionResult Dashboard() => View();
        public IActionResult Appointments() => View();
        public IActionResult Patients() => View();
        public IActionResult Examination() => View();
        public IActionResult Prescriptions() => View();
        public IActionResult Schedule() => View();
        public IActionResult Followups() => View();
    }
}