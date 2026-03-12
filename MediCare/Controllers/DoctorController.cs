using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class DoctorController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
        
        public IActionResult Appointments()
        {
            return View();
        }

        public IActionResult Patients()
        {
            return View();
        }

        public IActionResult Examination()
        {
            return View();
        }

        public IActionResult Prescriptions()
        {
            return View();
        }

        public IActionResult Schedule()
        {
            return View();
        }

        public IActionResult FollowUps()
        {
            return View();
        }
    }
}
