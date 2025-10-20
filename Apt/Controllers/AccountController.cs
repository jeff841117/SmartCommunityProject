using Apt.Models;
using Apt.Models.ViewModels;
using Apt.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Apt.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        [HttpGet]
        public IActionResult RegisterTenant()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> RegisterTenant(RegisterTenantViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    Address = model.Address,
                    PhoneNumber = model.Phone,
                    EmailConfirmed = true,        //  註冊時自動驗證
                    CreatedAt = DateTime.UtcNow,  // 設置建立時間
                    LastLoginAt = DateTime.UtcNow // 首次登入時間
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // 確保單一角色
                    await _userManager.AddToRoleAsync(user, "Tenant");

                    var token = await _jwtService.GenerateTokenAsync(user, _userManager);
                    SetAuthCookie(token);
                    return RedirectToAction("Index1", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterAdmin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAdmin(RegisterAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    Address = model.Address,
                    PhoneNumber = model.Phone,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");

                    var token = await _jwtService.GenerateTokenAsync(user, _userManager);
                    SetAuthCookie(token);
                    return RedirectToAction("Index", "Admin");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null)
                {
                    //  更新最後登入時間（使用可空 DateTime?）
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                    if (result.Succeeded)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        var primaryRole = roles.FirstOrDefault();

                        if (!string.IsNullOrEmpty(primaryRole))
                        {
                            var token = await _jwtService.GenerateTokenAsync(user, _userManager);
                            SetAuthCookie(token);

                            //  精確重定向
                            return primaryRole switch
                            {
                                "Tenant" => RedirectToAction("Index1", "Home"),
                                "Admin" => RedirectToAction("Index", "Admin"),
                                _ => RedirectToAction("Index", "Home")
                            };
                        }
                        ModelState.AddModelError("", "用戶沒有分配角色");
                    }
                    else
                    {
                        ModelState.AddModelError("", "無效的用戶名或密碼");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "無效的用戶名或密碼");
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // ✅ 清除 JWT Cookie
            Response.Cookies.Delete("jwt");

            // ✅ 登出後重定向到 Home/Index
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public IActionResult VerifyEmail()
        {
            return View();
        }
        public IActionResult ChangePassword()
        {
            return View();
        }
       
        private void SetAuthCookie(string token)
        {
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddMinutes(60)
            });
        }

        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            var userName = User.Identity?.Name;
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            ViewBag.UserInfo = $"用戶：{userName}，角色：{role}，ID：{userId}";
            return View();
        }
    }
}