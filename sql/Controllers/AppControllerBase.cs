using Microsoft.AspNetCore.Mvc;
using sql.Services;

namespace sql.Controllers
{
    // 這個基底 Controller 專門收斂「目前使用者」與「權限檢查」的共用邏輯。
    // 目的是避免每個 Controller 都自己重寫登入檢查、管理員檢查與導頁規則。
    public abstract class AppControllerBase : Controller
    {
        private readonly CurrentUserService _currentUserService;

        protected AppControllerBase(CurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected CurrentUser GetCurrentUserInfo()
        {
            return _currentUserService.GetCurrentUser();
        }

        protected IActionResult? EnsureAuthenticatedRedirect()
        {
            return GetCurrentUserInfo().IsAuthenticated
                ? null
                : RedirectToAction("Login", "Account");
        }

        protected IActionResult? EnsureManagerRedirect(string noPermissionMessage = "您沒有管理員權限")
        {
            var currentUser = GetCurrentUserInfo();
            if (!currentUser.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!currentUser.IsManager)
            {
                TempData["Error"] = noPermissionMessage;
                return RedirectToAction("Reservation", "Equipment");
            }

            return null;
        }
    }
}
