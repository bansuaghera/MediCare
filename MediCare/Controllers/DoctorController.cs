using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class DoctorController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
