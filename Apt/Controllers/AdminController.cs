using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apt.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            ViewBag.Welcome = $"歡迎管理員 {username}";
            return View();
        }
    }
}