using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using sql.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace sql.Controllers
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
            DBmanager dbmanager = new DBmanager();
            List<account> accounts = dbmanager.getAccounts();
            ViewBag.accounts = accounts;
            return View();
        }

        [HttpPost]
        public JsonResult UpdateAccount(int id, string password, string email, string phone)
        {
            try
            {
                Console.WriteLine($"接收到更新請求 - ID: {id}, Password: {password}, Email: {email}, Phone: {phone}");

                // 驗證必要欄位
                if (string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "密碼不能為空" });
                }

                // 創建帳號物件
                var updatedUser = new account
                {
                    id = id,
                    password = password,
                    email = email,
                    phone = phone
                };

                // 呼叫 DBManager 的更新方法
                DBmanager dbmanager = new DBmanager();
                dbmanager.updateAccount(updatedUser);

                Console.WriteLine("更新成功");
                return Json(new { success = true, message = "更新成功" });
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL 錯誤: {sqlEx.Message}, 錯誤代碼: {sqlEx.Number}");
                return Json(new
                {
                    success = false,
                    message = $"資料庫錯誤: {sqlEx.Message}",
                    errorCode = sqlEx.Number
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"一般錯誤: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = $"更新失敗: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }

        public IActionResult addAccount()
        {
            return View();
        }

        [HttpPost]
        public IActionResult addAccount(account user)
        {

            DBmanager dbmanager = new DBmanager();
            try
            {
                dbmanager.newAccount(user);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Privacy()
        {
            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
