using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
