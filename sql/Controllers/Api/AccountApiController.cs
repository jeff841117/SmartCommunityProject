using Microsoft.AspNetCore.Mvc;
using sql.Models;
using sql.Services;

namespace sql.Controllers.Api
{
    [ApiController]
    [Route("api/account")]
    public class AccountApiController : ControllerBase
    {
        private readonly AccountService _accountService;
        private readonly CurrentUserService _currentUserService;

        public AccountApiController(AccountService accountService, CurrentUserService currentUserService)
        {
            _accountService = accountService;
            _currentUserService = currentUserService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] ApiLoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseFactory.OperationFailure("請確認登入資料是否填寫完整。"));
            }

            var user = _accountService.ValidateUser(request.UserName, request.Password);
            if (user == null)
            {
                return Unauthorized(ApiResponseFactory.OperationFailure("帳號或密碼錯誤。"));
            }

            HttpContext.Session.SetInt32("UserId", user.id);
            HttpContext.Session.SetString("UserName", user.userName);
            HttpContext.Session.SetString("UserRole", user.role ?? "user");

            return Ok(ApiResponseFactory.DataSuccess(new CurrentUserResponse
            {
                IsLoggedIn = true,
                UserId = user.id,
                UserName = user.userName,
                Role = user.role ?? "user",
                IsManager = user.role == "manager" || user.role == "admin"
            }, "登入成功。"));
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(ApiResponseFactory.OperationSuccess("登出成功。"));
        }

        [HttpGet("current-user")]
        public IActionResult GetCurrentUser()
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (!currentUser.IsAuthenticated)
            {
                return Ok(new CurrentUserResponse
                {
                    IsLoggedIn = false
                });
            }

            return Ok(new CurrentUserResponse
            {
                IsLoggedIn = true,
                UserId = currentUser.UserId,
                UserName = currentUser.UserName,
                Role = currentUser.Role,
                IsManager = currentUser.IsManager
            });
        }

        [HttpPost("forgot-password/request-code")]
        public IActionResult RequestPasswordResetCode([FromBody] ApiForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseFactory.OperationFailure("請確認電子郵件格式是否正確。"));
            }

            var result = _accountService.RequestPasswordReset(request.Email);
            if (!result.Success)
            {
                return BadRequest(ApiResponseFactory.OperationFailure(result.Message));
            }

            return Ok(ApiResponseFactory.DataSuccess(result, result.Message));
        }

        [HttpPost("forgot-password/reset")]
        public IActionResult ResetPassword([FromBody] ApiResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseFactory.OperationFailure("請確認驗證碼與新密碼欄位是否填寫完整。"));
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(ApiResponseFactory.OperationFailure("新密碼與確認密碼不一致。"));
            }

            var result = _accountService.ResetPassword(request.Email, request.VerificationCode, request.NewPassword);
            if (!result.Success)
            {
                return BadRequest(ApiResponseFactory.OperationFailure(result.Message));
            }

            return Ok(ApiResponseFactory.DataSuccess(result, result.Message));
        }
    }
}
