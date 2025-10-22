using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCommunity.Data;
using SmartCommunity.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace SmartCommunity.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly SmartCommunityContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public PaymentsController(SmartCommunityContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private async Task<User?> GetCurrentResidentAsync()
        {
            var aspId = _userManager.GetUserId(User);
            if (aspId == null) return null;
            return await _db.Set<User>().FirstOrDefaultAsync(u => u.AspNetUserId == aspId);
        }

        // 住戶：繳費紀錄（已繳）
        [HttpGet]
        public async Task<IActionResult> MyHistory()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index");

            var me = await GetCurrentResidentAsync();
            if (me == null) return Forbid();

            var list = await _db.Payments
                .Include(p => p.Bill).ThenInclude(b => b.FeeItem)
                .Where(p => p.Bill.UserID == me.UserID)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.Room = me.RoomNumber;
            return View("MyHistory", list);
        }

        // ★ 住戶：付款（只收 billId + method）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int billId, string method)
        {
            if (User.IsInRole("Admin")) return BadRequest("管理者請用後台。");

            var me = await GetCurrentResidentAsync();
            if (me == null) return Forbid();

            // 只能操作自己的未繳帳單
            var bill = await _db.Bills
                .Include(b => b.FeeItem)
                .FirstOrDefaultAsync(b => b.BillID == billId && b.UserID == me.UserID);

            if (bill == null) return NotFound("找不到帳單。");
            if (bill.Status == "已繳")
            {
                TempData["Info"] = "此帳單已繳清。";
                return RedirectToAction("MyUnpaid", "Bills");
            }

            // 付款方式白名單
            var allowed = new[] { "現金", "轉帳(ATM)", "LinePay" };
            if (!allowed.Contains(method))
            {
                TempData["Error"] = "付款方式不正確。";
                return RedirectToAction("MyUnpaid", "Bills");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Payments.Add(new Payment
                {
                    BillID = bill.BillID,
                    Amount = bill.Amount,           // 一律用帳單金額
                    PaymentMethod = method,         // 付款方式
                    PaymentDate = DateTime.Now      // 一律用現在時間
                });

                bill.Status = "已繳";

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = $"已完成付款：{bill.FeeItem?.ItemName} / {bill.Amount:#,0.##}";
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "付款失敗，請稍後再試。";
            }

            return RedirectToAction("MyUnpaid", "Bills");
        }

        // 管理者總覽
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int? userId = null)
        {
            var q = _db.Payments
                .Include(p => p.Bill).ThenInclude(b => b.User)
                .Include(p => p.Bill).ThenInclude(b => b.FeeItem)
                .AsQueryable();

            if (userId.HasValue) q = q.Where(p => p.Bill.UserID == userId.Value);

            var list = await q.OrderByDescending(p => p.PaymentDate).ToListAsync();
            return View(list);
        }
    }
}