using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCommunity.Data;
using SmartCommunity.Models;

namespace SmartCommunity.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly SmartCommunityContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminUsersController(SmartCommunityContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // 🟢 住戶清單
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Residents
                .OrderBy(u => u.RoomNumber)
                .AsNoTracking()
                .ToListAsync();

            return View(list);
        }

        // 🟢 編輯頁面
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Residents.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // 🟢 儲存修改
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User input)
        {
            if (id != input.UserID) return BadRequest();

            var dbUser = await _db.Residents.FirstOrDefaultAsync(u => u.UserID == id);
            if (dbUser == null) return NotFound();

            // 更新住戶資料
            dbUser.UserName = input.UserName;
            dbUser.RoomNumber = input.RoomNumber;
            dbUser.Email = input.Email;
            dbUser.Phone = input.Phone;

            // 同步 Identity 帳號（登入信箱）
            if (!string.IsNullOrEmpty(dbUser.AspNetUserId))
            {
                var identityUser = await _userManager.FindByIdAsync(dbUser.AspNetUserId);
                if (identityUser != null)
                {
                    identityUser.Email = input.Email;
                    identityUser.UserName = input.Email;
                    identityUser.NormalizedEmail = input.Email?.ToUpperInvariant();
                    identityUser.NormalizedUserName = input.Email?.ToUpperInvariant();
                    await _userManager.UpdateAsync(identityUser);
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ 已成功更新用戶資料！";
            return RedirectToAction(nameof(Index));
        }

        // 🔴 刪除住戶（由 DB 級聯刪除 Bills/Payments；可選同時刪除 Identity 帳號）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId, bool alsoDeleteAccount = true)
        {
            // 先抓一份快照，取 AspNetUserId 之後刪 Identity 用
            var snapshot = await _db.Residents
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (snapshot == null)
            {
                TempData["Error"] = "找不到要刪除的住戶。";
                return RedirectToAction(nameof(Index));
            }

            var aspNetUserId = snapshot.AspNetUserId;

            // 使用 key-only 實體進行刪除（單一實體，避免 Attach/Remove 不同物件）
            var stub = new User { UserID = userId };
            _db.Entry(stub).State = EntityState.Deleted;

            try
            {
                // 這裡會觸發你在資料庫已設定的 ON DELETE CASCADE 刪掉 Bills 與 Payments
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"刪除住戶時發生錯誤：{ex.GetBaseException().Message}";
                return RedirectToAction(nameof(Index));
            }

            // 同步刪除 Identity 帳號（若有綁定）
            if (alsoDeleteAccount && !string.IsNullOrEmpty(aspNetUserId))
            {
                var account = await _userManager.FindByIdAsync(aspNetUserId);
                if (account != null)
                {
                    var result = await _userManager.DeleteAsync(account);
                    if (!result.Succeeded)
                    {
                        TempData["Error"] = "住戶資料已刪除，但刪除登入帳號失敗：" +
                                            string.Join("; ", result.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            TempData["Success"] = "✅ 已刪除住戶與其所有帳單與繳費紀錄。";
            return RedirectToAction(nameof(Index));
        }
    }
}