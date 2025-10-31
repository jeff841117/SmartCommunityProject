using System.Diagnostics;
using DeliveySystem2.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeliveySystem2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Index(LoginView model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            DBmanager db = new DBmanager();
            string role = db.CheckUser(model.account, model.password);
            if (string.IsNullOrEmpty(role))
            {
                ViewBag.ErrorMessage = "帳號或密碼錯誤，請重新輸入";
                return View(model);
            }
            if (role == "user")
            {
                return RedirectToAction("Index", "User");
            }
            else if (role == "security")
            {
                return RedirectToAction("Index", "Security");
            }
            else
            {
                ViewBag.ErrorMessage = "未知人員，請聯絡管理員";
                return View(model);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
