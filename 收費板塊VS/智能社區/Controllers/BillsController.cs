using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCommunity.Data;
using SmartCommunity.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCommunity.Controllers
{
    [Authorize]
    public class BillsController : Controller
    {
        private readonly SmartCommunityContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public BillsController(SmartCommunityContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // 取得目前登入的住戶
        private async Task<User?> GetCurrentResidentAsync()
        {
            var aspId = _userManager.GetUserId(User);
            if (aspId == null) return null;

            // 你有 DbSet<User> Residents，就用它；若沒有可改成 _db.Set<User>()
            return await _db.Residents.FirstOrDefaultAsync(u => u.AspNetUserId == aspId);
        }

        // =================== 住戶：自己的未繳帳單 ===================
        [HttpGet]
        public async Task<IActionResult> MyUnpaid()
        {
            // 管理者導回管理頁
            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(Index));

            var me = await GetCurrentResidentAsync();
            if (me == null)
            {
                ViewBag.Message = "無法找到對應住戶資料，請聯絡管理員。";
                return View("Error", new ErrorViewModel { Message = "無法找到對應住戶資料，請聯絡管理員。" });
            }

            var bills = await _db.Bills
                .Include(b => b.FeeItem)
                .Where(b => b.UserID == me.UserID && b.Status == "未繳")
                .OrderBy(b => b.BillID)
                .ToListAsync();

            ViewBag.Room = me.RoomNumber;
            // 提供 LinePay QR 圖片路徑（把檔案放在 wwwroot/images/linepay_qr.png）
            ViewBag.LinePayQr = Url.Content("~/images/linepay_qr.png");
            return View("MyUnpaid", bills);
        }

        // =================== 管理者：帳單總覽（可依 userId 篩選） ===================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Index(int? userId = null)
        {
            var query = _db.Bills
                .Include(b => b.User)
                .Include(b => b.FeeItem)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(b => b.UserID == userId.Value);

            var list = await query
                .OrderByDescending(b => b.BillID)
                .ToListAsync();

            return View(list);
        }

        // =================== 管理者：一鍵查「所有未繳」 ===================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> UnpaidAll()
        {
            var list = await _db.Bills
                .Include(b => b.User)
                .Include(b => b.FeeItem)
                .Where(b => b.Status == "未繳")
                .OrderBy(b => b.User.RoomNumber)
                .ThenBy(b => b.FeeItem.ItemName)
                .ToListAsync();

            return View(list); // 對應 Views/Bills/UnpaidAll.cshtml
        }
    }
}