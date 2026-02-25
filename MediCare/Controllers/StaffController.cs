using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult RegisterPatient()
        {
            return View();
        }
        public IActionResult Patients()
        {
            return View();
        }
        public IActionResult BookAppointment()
        {
            return View();
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
            return View();
        }
        public IActionResult Schedule()
        {
            return View();
        }
    }
}
