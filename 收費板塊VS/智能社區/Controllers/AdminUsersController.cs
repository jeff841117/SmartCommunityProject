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

        // 🟢 顯示所有住戶
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Residents.OrderBy(u => u.RoomNumber).ToListAsync();
            return View(list);
        }

        // 🟢 編輯頁面
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _db.Residents.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // 🟢 儲存修改
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User input)
        {
            if (id != input.UserID)
                return BadRequest();

            var dbUser = await _db.Residents.FirstOrDefaultAsync(u => u.UserID == id);
            if (dbUser == null)
                return NotFound();

            // 更新住戶資料表
            dbUser.UserName = input.UserName;
            dbUser.RoomNumber = input.RoomNumber;
            dbUser.Email = input.Email;
            dbUser.Phone = input.Phone;

            // 同步更新 Identity 帳號（登入信箱）
            if (!string.IsNullOrEmpty(dbUser.AspNetUserId))
            {
                var identityUser = await _userManager.FindByIdAsync(dbUser.AspNetUserId);
                if (identityUser != null)
                {
                    identityUser.Email = input.Email;
                    identityUser.UserName = input.Email;
                    identityUser.NormalizedEmail = input.Email.ToUpperInvariant();
                    identityUser.NormalizedUserName = input.Email.ToUpperInvariant();
                    await _userManager.UpdateAsync(identityUser);
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "✅ 已成功更新用戶資料！";
            return RedirectToAction(nameof(Index));
        }
    }
}