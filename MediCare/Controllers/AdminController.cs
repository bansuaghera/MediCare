using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult Patient()
        {
            return View();
        }
        public IActionResult Doctors()
        {
            return View();
        }
        public IActionResult Staff()
        {
            return View();
        }
        public IActionResult OPDSchedule()
        {
            return View();
        }

        public IActionResult Appointments()
        {
            return View();
        }

        public IActionResult Prescriptions()
        {
            return View();
        }

        public IActionResult TokenQueue()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult Medicines()
        {
            return View();
        }

        public IActionResult Templates()
        {
            return View();
        }
        public IActionResult AddPatient()
        {
            return View();
        }
        public IActionResult AddStaff()
        {
            return View();
        }
        public IActionResult AddOPDSchedule()
        {
            return View();
        }
        public IActionResult AddMedicine()
        {
            return View();
        }
        public IActionResult AddTemplate()
            {
                return View();
        }
        public IActionResult AddDoctor()
        {
            return View();
        }


    }
}
