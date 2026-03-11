using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MediCare.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult Doctors()
        {
            return View();
        }

        public IActionResult BookAppointment()
        {
            return View();
        }

        public IActionResult MyAppointments()
        {
            return View();
        }

        public IActionResult Prescriptions()
        {
            return View();
        }
        public IActionResult TokenStatus()
        {
            return View();
        }
        public IActionResult History()
        {
            return View();
        }
        public IActionResult Profile()
        {
            return View();
        }
    }
}
