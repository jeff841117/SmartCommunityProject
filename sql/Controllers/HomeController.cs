using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;

namespace sql.Controllers
{
    public class HomeController : AppControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AccountService _accountService;
        private readonly AdminActionLogService _adminActionLogService;

        public HomeController(
            ILogger<HomeController> logger,
            AccountService accountService,
            AdminActionLogService adminActionLogService,
            CurrentUserService currentUserService)
            : base(currentUserService)
        {
            _logger = logger;
            _accountService = accountService;
            _adminActionLogService = adminActionLogService;
        }

        public IActionResult Index([FromQuery] AccountManagementFilter filter)
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            var currentUser = GetCurrentUserInfo();
            var viewModel = new AccountManagementPageViewModel
            {
                Filter = filter,
                QueryResult = _accountService.GetAccounts(filter),
                CurrentUserName = currentUser.UserName,
                IsManager = currentUser.IsManager
            };

            return View(viewModel);
        }

        public IActionResult AdminActionLogs([FromQuery] AdminActionLogFilter filter)
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            var currentUser = GetCurrentUserInfo();

            var viewModel = new AdminActionLogPageViewModel
            {
                Filter = filter,
                ActionTypeOptions = AdminActionLogDisplayHelper.GetActionTypeOptions(),
                TargetTypeOptions = AdminActionLogDisplayHelper.GetTargetTypeOptions(),
                QueryResult = _adminActionLogService.GetLogs(filter),
                CurrentUserName = currentUser.UserName,
                IsManager = currentUser.IsManager
            };

            return View(viewModel);
        }

        public IActionResult ExportAdminActionLogs([FromQuery] AdminActionLogFilter filter)
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            var fileBytes = _adminActionLogService.ExportLogsAsCsv(filter);
            var fileName = $"admin-action-logs-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            return File(fileBytes, "text/csv; charset=utf-8", fileName);
        }

        [HttpPost]
        public JsonResult UpdateAccount(UpdateAccountFormViewModel form)
        {
            if (EnsureManagerRedirect() != null)
            {
                return Json(ApiResponseFactory.OperationFailure("您沒有管理員權限"));
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

                return Json(ApiResponseFactory.OperationFailure(firstError ?? "請確認更新欄位是否填寫正確"));
            }

            var updatedUser = new account
            {
                id = form.Id,
                password = form.Password ?? string.Empty,
                age = form.Age,
                email = form.Email,
                phone = form.Phone
            };

            var result = _accountService.UpdateAccount(updatedUser);
            return Json(result);
        }

        [HttpPost]
        public JsonResult ToggleAccountStatus(int id, bool isActive)
        {
            if (EnsureManagerRedirect() != null)
            {
                return Json(ApiResponseFactory.OperationFailure("您沒有管理員權限"));
            }

            var currentUser = GetCurrentUserInfo();
            var targetUser = _accountService.GetAllAccounts().FirstOrDefault(user => user.id == id);
            if (targetUser == null)
            {
                return Json(ApiResponseFactory.OperationFailure("找不到要更新的帳號"));
            }

            if (!isActive && string.Equals(targetUser.userName, currentUser.UserName, StringComparison.OrdinalIgnoreCase))
            {
                return Json(ApiResponseFactory.OperationFailure("不能停用目前登入中的帳號"));
            }

            var result = _accountService.ToggleAccountStatus(id, isActive);
            return Json(result);
        }

        public IActionResult addAccount()
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            return View(new AddAccountFormViewModel());
        }

        [HttpPost]
        public JsonResult CreateAccountModal(AddAccountFormViewModel form)
        {
            if (EnsureManagerRedirect() != null)
            {
                return Json(ApiResponseFactory.OperationFailure("您沒有管理員權限"));
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

                return Json(ApiResponseFactory.OperationFailure(firstError ?? "請確認帳號資料是否填寫正確"));
            }

            var user = new account
            {
                userName = form.UserName,
                password = form.Password ?? string.Empty,
                age = form.Age,
                email = form.Email,
                phone = form.Phone,
                role = "user",
                isActive = true
            };

            var created = _accountService.CreateAccount(user);
            return Json(created
                ? ApiResponseFactory.OperationSuccess("新增帳號成功")
                : ApiResponseFactory.OperationFailure("新增帳號失敗"));
        }

        [HttpPost]
        public IActionResult addAccount(AddAccountFormViewModel form)
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            if (!ModelState.IsValid)
            {
                form.ErrorMessage = "請確認表單欄位是否填寫正確";
                return View(form);
            }

            var user = new account
            {
                userName = form.UserName,
                password = form.Password ?? string.Empty,
                age = form.Age,
                email = form.Email,
                phone = form.Phone,
                role = "user",
                isActive = true
            };

            var created = _accountService.CreateAccount(user);
            if (!created)
            {
                form.ErrorMessage = "新增帳號失敗";
                return View(form);
            }

            return RedirectToAction("Login", "Account");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}


