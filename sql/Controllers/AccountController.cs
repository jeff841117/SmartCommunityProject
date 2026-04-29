using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using sql.Models;
using Microsoft.Data.SqlClient;

namespace sql.Controllers
{
    public class AccountController : Controller
    {
        private readonly DBmanager _dbManager;

        public AccountController()
        {
            _dbManager = new DBmanager();
        }

        // 登入頁面
        public IActionResult Login()
        {
            return View();
        }

        // 忘記密碼頁面
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "請輸入帳號和密碼";
                return View();
            }

            var user = _dbManager.ValidateUser(username, password);
            if (user != null)
            {
                // 儲存使用者資訊到 Session
                HttpContext.Session.SetInt32("UserId", user.id);
                HttpContext.Session.SetString("UserName", user.userName);
                HttpContext.Session.SetString("UserRole", user.role ?? "user"); // 儲存使用者角色

                // 根據角色決定跳轉頁面
                if (user.role == "manager" || user.role == "admin")
                {
                    // 管理者跳轉到 Equipment/Index
                    return RedirectToAction("Index", "Equipment");
                }
                else
                {
                    // 一般使用者跳轉到 Equipment/Reservation
                    return RedirectToAction("Reservation", "Equipment");
                }
            }
            else
            {
                ViewBag.Error = "帳號或密碼錯誤";
                return View();
            }
        }

        // 登出
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // 註冊頁面 (使用您現有的 addAccount)
        public IActionResult Register()
        {
            return RedirectToAction("addAccount", "Home");
        }

        // 檢查當前登入使用者是否為管理者
        public bool IsCurrentUserManager()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "manager" || userRole == "admin";
        }

        // 取得當前使用者資訊
        public (int? userId, string userName, string role) GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("UserRole") ?? "user";

            return (userId, userName, role);
        }
    }
}
