using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;

namespace sql.Controllers
{
    // AccountController 主要處理登入 / 登出 / 忘記密碼入口。
    // 這一輪把 ForgotPassword 從「只有申請驗證碼」推進成可真正重設密碼的流程。
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly CurrentUserService _currentUserService;

        public AccountController(AccountService accountService, CurrentUserService currentUserService)
        {
            _accountService = accountService;
            _currentUserService = currentUserService;
        }

        public IActionResult Login()
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser.IsAuthenticated)
            {
                return currentUser.IsManager
                    ? RedirectToAction("Index", "Equipment")
                    : RedirectToAction("Reservation", "Equipment");
            }

            return View(new LoginFormViewModel());
        }

        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordFormViewModel());
        }

        [HttpGet]
        public JsonResult PasswordResetEmailDebug()
        {
            var diagnostic = _accountService.GetPasswordResetEmailDiagnosticInfo();
            return Json(diagnostic);
        }

        [HttpPost]
        public IActionResult Login(LoginFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                form.ErrorMessage = "請輸入帳號和密碼";
                return View(form);
            }

            // 登入驗證改成走 AccountService，
            // 這樣帳號相關資料存取就能集中在帳號模組內。
            var user = _accountService.ValidateUser(form.UserName, form.Password);
            if (user == null)
            {
                form.ErrorMessage = "帳號或密碼錯誤";
                return View(form);
            }

            // 登入成功後，把最基本的識別資訊放進 Session。
            HttpContext.Session.SetInt32("UserId", user.id);
            HttpContext.Session.SetString("UserName", user.userName);
            HttpContext.Session.SetString("UserRole", user.role ?? "user");

            // 管理者與一般會員進不同入口頁。
            if (user.role == "manager" || user.role == "admin")
            {
                return RedirectToAction("Index", "Equipment");
            }

            return RedirectToAction("Reservation", "Equipment");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult Register()
        {
            return RedirectToAction("addAccount", "Home");
        }

        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordFormViewModel form)
        {
            if (form.Step == "reset")
            {
                return HandleResetPassword(form);
            }

            return HandleRequestPasswordReset(form);
        }

        // 申請驗證碼這一步只需要 Email。
        private IActionResult HandleRequestPasswordReset(ForgotPasswordFormViewModel form)
        {
            if (string.IsNullOrWhiteSpace(form.Email))
            {
                form.ErrorMessage = "請輸入正確的電子郵件";
                form.Step = "request";
                return View(form);
            }

            var result = _accountService.RequestPasswordReset(form.Email);
            if (!result.Success)
            {
                form.ErrorMessage = result.Message;
                form.Step = "request";
                return View(form);
            }

            form.SuccessMessage = result.Message;
            form.DebugCode = result.DebugCode;
            form.Step = "reset";
            return View(form);
        }

        // 重設密碼這一步需要 Email、驗證碼、新密碼與確認密碼。
        private IActionResult HandleResetPassword(ForgotPasswordFormViewModel form)
        {
            if (string.IsNullOrWhiteSpace(form.Email) ||
                string.IsNullOrWhiteSpace(form.VerificationCode) ||
                string.IsNullOrWhiteSpace(form.NewPassword) ||
                string.IsNullOrWhiteSpace(form.ConfirmPassword))
            {
                form.ErrorMessage = "請完整輸入電子郵件、驗證碼與新密碼";
                form.Step = "reset";
                return View(form);
            }

            if (form.NewPassword != form.ConfirmPassword)
            {
                form.ErrorMessage = "兩次輸入的新密碼不一致";
                form.Step = "reset";
                return View(form);
            }

            var result = _accountService.ResetPassword(form.Email, form.VerificationCode, form.NewPassword);
            if (!result.Success)
            {
                form.ErrorMessage = result.Message;
                form.Step = "reset";
                return View(form);
            }

            return View(new ForgotPasswordFormViewModel
            {
                Email = form.Email,
                SuccessMessage = result.Message,
                Step = "request"
            });
        }

        // 這裡開始改成透過 CurrentUserService 判斷角色，
        // 避免 Controller 自己到處重複讀 Session。
        public bool IsCurrentUserManager()
        {
            return _currentUserService.IsManager();
        }

        public (int? userId, string userName, string role) GetCurrentUser()
        {
            var currentUser = _currentUserService.GetCurrentUser();
            return (currentUser.UserId, currentUser.UserName, currentUser.Role);
        }
    }
}
