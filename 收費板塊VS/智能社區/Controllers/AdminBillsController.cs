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

        // 取得下拉資料
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

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await BuildVmAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBillVm vm)
        {
            // 重新帶下拉資料（回填）
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

            // 避免重複：同住戶 + 同項目 + 未繳
            var exists = await _db.Bills.AnyAsync(b =>
                b.UserID == vm.UserId &&
                b.FeeItemID == vm.FeeItemId &&
                b.Status == StatusUnpaid);

            if (exists)
            {
                vm.Error = $"已存在 {user.UserName}（{user.RoomNumber}） 的「{fee.ItemName}」未繳帳單，請先處理或改項目。";
                return View(vm);
            }

            // 計算金額
            var calcResult = TryCalcAmount(fee.ItemName, fee.UnitPrice, vm.Usage, vm.Amount, out var amount);
            if (!calcResult)
            {
                vm.Error = "請輸入度數或金額。";
                return View(vm);
            }

            try
            {
                // 寫入帳單（無到期日版本）
                _db.Bills.Add(new Bill
                {
                    UserID = vm.UserId,
                    FeeItemID = vm.FeeItemId,
                    Amount = amount,
                    Status = StatusUnpaid
                });

                await _db.SaveChangesAsync();

                TempData["Success"] = $"已新增：{user.UserName}（{user.RoomNumber}） / {fee.ItemName} / 金額 {amount}";
                // PRG：成功就重導，避免 F5 重送
                return RedirectToAction(nameof(Create));
            }
            catch (DbUpdateException)
            {
                vm.Error = "儲存失敗，請稍後再試或聯絡系統管理員。";
                return View(vm);
            }
        }

        /// <summary>
        /// 金額計算：管理費固定 3000；水/電：優先用度數*單價，否則用手動金額。
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
}