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


    }
}
