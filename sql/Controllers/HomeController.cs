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

        public IActionResult Index()
        {
            var accessRedirect = EnsureManagerRedirect();
            if (accessRedirect != null)
            {
                return accessRedirect;
            }

            var currentUser = GetCurrentUserInfo();

            // 帳號列表現在改成走 AccountService，
            // 這樣 Controller 就不需要自己 new DBmanager。
            var viewModel = new AccountManagementPageViewModel
            {
                Accounts = _accountService.GetAllAccounts(),
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
                return Json(ApiResponseFactory.OperationFailure("請確認更新欄位是否填寫正確"));
            }

            Console.WriteLine($"接收到更新請求 - ID: {form.Id}, Password: {form.Password}, Email: {form.Email}, Phone: {form.Phone}");

            var updatedUser = new account
            {
                id = form.Id,
                password = form.Password,
                email = form.Email,
                phone = form.Phone
            };

            var result = _accountService.UpdateAccount(updatedUser);
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
                password = form.Password,
                age = form.Age,
                email = form.Email,
                phone = form.Phone
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

            // 建立帳號也改成走 AccountService，
            // 後面如果要補 email 驗證或預設角色規則，就有固定入口可以加。
            var user = new account
            {
                userName = form.UserName,
                password = form.Password,
                age = form.Age,
                email = form.Email,
                phone = form.Phone
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
