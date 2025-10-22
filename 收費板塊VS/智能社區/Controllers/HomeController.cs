using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartCommunity.Data;
using SmartCommunity.Models;
using SmartCommunity.Models.ViewModels;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCommunity.Controllers
{
    public class HomeController : Controller
    {
        private readonly SmartCommunityContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(SmartCommunityContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ===== 首頁 =====
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // ✅ 已登入的住戶 → 直接導到自己的未繳帳單
            if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin"))
            {
                return RedirectToAction("MyUnpaid", "Bills");
            }

            // ✅ 管理者或未登入 → 顯示房號查詢表單
            return View(new QueryRoomVm());
        }

        /// <summary>
        /// 由首頁表單轉到「未繳帳單」列表（BillsController.Index）
        /// 僅供管理者使用
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GoBills(QueryRoomVm vm)
        {
            // 一般住戶不該透過這個查
            if (!User.IsInRole("Admin"))
                return RedirectToAction("MyUnpaid", "Bills");

            if (string.IsNullOrWhiteSpace(vm.RoomInput))
            {
                TempData["Error"] = "請輸入房號（例如 A101 或 101）。";
                return RedirectToAction(nameof(Index));
            }

            var room = NormalizeRoom(vm.RoomInput);
            var user = _db.Residents.FirstOrDefault(u => u.RoomNumber == room);

            if (user == null)
            {
                TempData["Error"] = $"找不到房號：{room}";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Index", "Bills", new { userId = user.UserID });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error(string? message = null)
        {
            var vm = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = message
            };
            return View(vm);
        }

        /// <summary>
        /// 由首頁表單轉到「繳費紀錄」頁（PaymentsController.Index）
        /// 僅供管理者使用
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GoPayments(QueryRoomVm vm)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("MyHistory", "Payments");

            if (string.IsNullOrWhiteSpace(vm.RoomInput))
            {
                TempData["Error"] = "請輸入房號（例如 A101）。";
                return RedirectToAction(nameof(Index));
            }

            var room = NormalizeRoom(vm.RoomInput);
            var user = _db.Residents.FirstOrDefault(u => u.RoomNumber == room);

            if (user == null)
            {
                TempData["Error"] = $"找不到房號：{room}";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Index", "Payments", new { userId = user.UserID });
        }

        /// <summary>
        /// 房號正規化：
        /// - "101"  -> "A101"（預設 A 棟）
        /// - "a101" -> "A101"
        /// - "b203" -> "B203"
        /// </summary>
        private static string NormalizeRoom(string? input)
        {
            var s = (input ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(s)) return s;

            if (s.Any(char.IsLetter)) return s;
            if (s.All(char.IsDigit) && (s.Length == 3 || s.Length == 4))
                return "A" + s;

            return s;
        }
    }
}