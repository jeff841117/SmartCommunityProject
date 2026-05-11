using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;

namespace sql.Controllers
{
    // AccountController 負責登入、登出與忘記密碼流程。
    // 真正的帳號規則放在 AccountService，這裡只負責表單與導頁。
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

        [HttpPost]
        public IActionResult Login(LoginFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                form.ErrorMessage = "請完整輸入帳號與密碼。";
                return View(form);
            }

            var loginResult = _accountService.ValidateLogin(form.UserName, form.Password ?? string.Empty);
            if (!loginResult.Success || loginResult.User == null)
            {
                form.ErrorMessage = loginResult.ErrorMessage;
                return View(form);
            }

            var user = loginResult.User;
            HttpContext.Session.SetInt32("UserId", user.id);
            HttpContext.Session.SetString("UserName", user.userName);
            HttpContext.Session.SetString("UserRole", user.role ?? "user");

            if (AccountDisplayHelper.IsManagerRole(user.role))
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
        public IActionResult ForgotPassword(ForgotPasswordFormViewModel form)
        {
            return form.Step == "reset"
                ? HandleResetPassword(form)
                : HandleRequestPasswordReset(form);
        }

        private IActionResult HandleRequestPasswordReset(ForgotPasswordFormViewModel form)
        {
            if (string.IsNullOrWhiteSpace(form.Email))
            {
                form.ErrorMessage = "請輸入註冊時使用的電子郵箱。";
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

        private IActionResult HandleResetPassword(ForgotPasswordFormViewModel form)
        {
            if (string.IsNullOrWhiteSpace(form.Email) ||
                string.IsNullOrWhiteSpace(form.VerificationCode) ||
                string.IsNullOrWhiteSpace(form.NewPassword) ||
                string.IsNullOrWhiteSpace(form.ConfirmPassword))
            {
                form.ErrorMessage = "請完整輸入電子郵箱、驗證碼與新密碼。";
                form.Step = "reset";
                return View(form);
            }

            if (form.NewPassword != form.ConfirmPassword)
            {
                form.ErrorMessage = "確認密碼與新密碼不一致。";
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

