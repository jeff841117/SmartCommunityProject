using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCommunity.Data;
using SmartCommunity.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            if (me == null)
            {
                TempData["Error"] = "找不到住戶身分，請重新登入。";
                return RedirectToAction("Index", "Home");
            }

            var list = await _db.Payments
                .Include(p => p.Bill).ThenInclude(b => b.FeeItem)
                .Where(p => p.Bill.UserID == me.UserID)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.Room = me.RoomNumber;
            return View("MyHistory", list);
        }

        // 住戶：付款（只接受自己的帳單）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay([FromForm] int billId)
        {
            // 管理者不得代繳
            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "管理者無法代替住戶繳費。";
                return RedirectToAction("Index", "AdminBills");
            }

            var me = await GetCurrentResidentAsync();
            if (me == null)
            {
                TempData["Error"] = "找不到住戶身分，請重新登入。";
                return RedirectToAction("Index", "Home");
            }

            if (billId <= 0)
            {
                TempData["Error"] = "付款參數遺失或不正確。";
                return RedirectToAction("MyUnpaid", "Bills");
            }

            // 從表單讀 method（多一層保險）
            var methodRaw = (Request.Form["method"].ToString() ?? "").Trim();
            var method = methodRaw switch
            {
                "現金" => "現金",
                "轉帳(ATM)" or "ATM" or "轉帳" => "轉帳(ATM)",
                "LinePay" or "LINEPAY" => "LinePay",
                _ => ""
            };

            if (string.IsNullOrEmpty(method))
            {
                TempData["Error"] = "付款方式不正確。";
                return RedirectToAction("MyUnpaid", "Bills");
            }

            // 只能操作自己的帳單
            var bill = await _db.Bills
                .Include(b => b.FeeItem)
                .FirstOrDefaultAsync(b => b.BillID == billId);

            if (bill == null)
            {
                TempData["Error"] = "找不到該帳單。";
                return RedirectToAction("MyUnpaid", "Bills");
            }

            if (bill.UserID != me.UserID)
            {
                TempData["Error"] = "您沒有權限支付此帳單。";
                return RedirectToAction("MyUnpaid", "Bills");
            }

            if (bill.Status == "已繳")
            {
                TempData["Info"] = "此帳單已繳清。";
                return RedirectToAction("MyUnpaid", "Bills");
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Payments.Add(new Payment
                {
                    BillID = bill.BillID,
                    Amount = bill.Amount,
                    PaymentMethod = method,
                    PaymentDate = DateTime.Now
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

        // 管理者：付款紀錄總覽（僅檢視）
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