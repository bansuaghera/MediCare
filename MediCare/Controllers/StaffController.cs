using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
