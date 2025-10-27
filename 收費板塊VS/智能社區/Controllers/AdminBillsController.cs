using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCommunity.Data;
using SmartCommunity.Models;

namespace SmartCommunity.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBillsController : Controller
    {
        private readonly SmartCommunityContext _db;
        private const string StatusUnpaid = "未繳";
        private const string StatusPaid = "已繳";

        public AdminBillsController(SmartCommunityContext db) => _db = db;

        // ===== 共用：建立下拉資料（建立帳單用） =====
        private async Task<CreateBillVm> BuildVmAsync()
        {
            var vm = new CreateBillVm
            {
                Users = await _db.Residents
                    .OrderBy(u => u.RoomNumber)
                    .Select(u => new ValueTuple<int, string>(
                        u.UserID, $"{u.UserName} ({u.RoomNumber})"))
                    .ToListAsync(),

                FeeItems = await _db.FeeItems
                    .OrderBy(f => f.ItemName)
                    .Select(f => new ValueTuple<int, string, decimal, string>(
                        f.FeeItemID, f.ItemName, f.UnitPrice, f.Unit ?? string.Empty))
                    .ToListAsync()
            };
            return vm;
        }

        // ===== 清單：查詢/管理帳單 =====
        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.Bills
                .Include(b => b.User)
                .Include(b => b.FeeItem)
                .AsNoTracking()
                .OrderByDescending(b => b.BillID)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(b =>
                    b.BillID.ToString().Contains(q) ||
                    b.Status.Contains(q) ||
                    (b.User != null && (b.User.UserName.Contains(q) || b.User.RoomNumber.Contains(q))) ||
                    (b.FeeItem != null && b.FeeItem.ItemName.Contains(q)));
            }

            var list = await query.ToListAsync();
            return View(list); // 對應 Views/AdminBills/Index.cshtml
        }

        // ===== 新增（GET）=====
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await BuildVmAsync());
        }

        // ===== 新增（POST）=====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBillVm vm)
        {
            // 回填下拉
            var baseVm = await BuildVmAsync();
            vm.Users = baseVm.Users;
            vm.FeeItems = baseVm.FeeItems;

            // 基本驗證
            if (vm.UserId <= 0 || vm.FeeItemId <= 0)
            {
                vm.Error = "請選擇住戶與收費項目。";
                return View(vm);
            }

            var user = await _db.Residents.FirstOrDefaultAsync(x => x.UserID == vm.UserId);
            var fee = await _db.FeeItems.FirstOrDefaultAsync(x => x.FeeItemID == vm.FeeItemId);
            if (user is null || fee is null)
            {
                vm.Error = "找不到住戶或收費項目。";
                return View(vm);
            }

            // 同住戶+同項目的未繳單避免重複
            var exists = await _db.Bills.AnyAsync(b =>
                b.UserID == vm.UserId &&
                b.FeeItemID == vm.FeeItemId &&
                b.Status == StatusUnpaid);

            if (exists)
            {
                vm.Error = $"已存在 {user.UserName}（{user.RoomNumber}） 的「{fee.ItemName}」未繳帳單，請先處理或改項目。";
                return View(vm);
            }

            // 金額計算
            if (!TryCalcAmount(fee.ItemName, fee.UnitPrice, vm.Usage, vm.Amount, out var amount))
            {
                vm.Error = "請輸入度數或金額。";
                return View(vm);
            }

            try
            {
                _db.Bills.Add(new Bill
                {
                    UserID = vm.UserId,
                    FeeItemID = vm.FeeItemId,
                    Amount = amount,
                    Status = StatusUnpaid
                });

                await _db.SaveChangesAsync();
                TempData["Success"] = $"已新增：{user.UserName}（{user.RoomNumber}） / {fee.ItemName} / 金額 {amount}";
                return RedirectToAction(nameof(Create)); // PRG
            }
            catch (DbUpdateException)
            {
                vm.Error = "儲存失敗，請稍後再試或聯絡系統管理員。";
                return View(vm);
            }
        }

        // ===== 刪除（POST）：依賴 DB 級聯刪付款紀錄 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            _db.Entry(new Bill { BillID = id }).State = EntityState.Deleted;

            try
            {
                await _db.SaveChangesAsync();
                TempData["Success"] = "✅ 帳單已刪除（含其所有付款紀錄）。";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"刪除失敗：{ex.GetBaseException().Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// 金額計算：管理費固定 3000；水/電優先用度數*單價，否則用手動金額。
        /// </summary>
        private static bool TryCalcAmount(string itemName, decimal unitPrice, decimal? usage, decimal? manualAmount, out decimal amount)
        {
            if (itemName == "管理費")
            {
                amount = 3000m;
                return true;
            }
            if (usage.HasValue && usage.Value > 0)
            {
                amount = unitPrice * usage.Value;
                return true;
            }
            if (manualAmount.HasValue && manualAmount.Value > 0)
            {
                amount = manualAmount.Value;
                return true;
            }
            amount = 0m;
            return false;
        }
    }

    // ===== 只保留建立用的 ViewModel =====
    public class CreateBillVm
    {
        public int UserId { get; set; }
        public int FeeItemId { get; set; }
        public decimal? Usage { get; set; }   // 水/電度數（可空）
        public decimal? Amount { get; set; }  // 手動金額（可空）
        public string? Error { get; set; }
        public List<(int, string)> Users { get; set; } = new();
        public List<(int, string, decimal, string)> FeeItems { get; set; } = new();
    }
}