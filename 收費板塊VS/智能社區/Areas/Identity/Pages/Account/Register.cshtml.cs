// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using SmartCommunity.Data;
using SmartCommunity.Models;

namespace SmartCommunity.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly SmartCommunityContext _db;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            SmartCommunityContext db)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required, Display(Name = "姓名")]
            public string UserName { get; set; }

            [Required, EmailAddress, Display(Name = "Email")]
            public string Email { get; set; }

            [Required, Display(Name = "房號")]
            public string RoomNumber { get; set; }

            [Required, StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password), Display(Name = "密碼")]
            public string Password { get; set; }

            [DataType(DataType.Password), Display(Name = "確認密碼")]
            [Compare("Password", ErrorMessage = "密碼與確認密碼不一致。")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            var user = CreateUser();

            // IdentityUser 登入帳號用 Email
            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("使用者註冊成功。");

                // ★ 將註冊者同步新增到你的住戶資料表
                // 如果你的 SmartCommunityContext 裡 DbSet 名稱是 Residents → _db.Residents
                // 如果你仍用 Users → _db.Set<User>()
                var residents = _db.Set<User>();
                residents.Add(new User
                {
                    AspNetUserId = user.Id,
                    UserName = Input.UserName,
                    RoomNumber = Input.RoomNumber,
                    Email = Input.Email
                });
                await _db.SaveChangesAsync();

                // ========== 信箱驗證流程 ==========
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    $"請點擊 <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>這裡</a> 驗證信箱。");

                // ========== 自動登入 ==========
                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                }
                else
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"無法建立 IdentityUser 實例。請確認 IdentityUser 有無參數建構子。");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("目前使用的 UserStore 不支援 Email。");
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}