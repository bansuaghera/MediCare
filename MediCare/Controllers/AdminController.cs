using Microsoft.AspNetCore.Mvc;

namespace MediCare.Controllers
{
    public class AdminController : Controller
    {
        private readonly Services.UserService _userService;

        public AdminController(Services.UserService userService)
        {
            _userService = userService;
        }
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

        public IActionResult EditMedicine(int id)
        {
            return View();
        }
        public IActionResult EditDoctor(int id) { return View(); }
        public IActionResult EditPatient(int id) { return View(); }
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
    }
}
