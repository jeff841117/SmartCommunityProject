using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using sql.Models;
using sql.Repositories;
using System.Security.Cryptography;

namespace sql.Services
{
    public class AccountService
    {
        private readonly AccountRepository _accountRepository;
        private readonly PasswordResetEmailBridge _passwordResetEmailBridge;
        private readonly IWebHostEnvironment _environment;
        private readonly PasswordResetEmailOptions _passwordResetEmailOptions;

        public AccountService(
            AccountRepository accountRepository,
            PasswordResetEmailBridge passwordResetEmailBridge,
            IWebHostEnvironment environment,
            IOptions<PasswordResetEmailOptions> passwordResetEmailOptions)
        {
            _accountRepository = accountRepository;
            _passwordResetEmailBridge = passwordResetEmailBridge;
            _environment = environment;
            _passwordResetEmailOptions = passwordResetEmailOptions.Value;
        }

        public List<account> GetAllAccounts()
        {
            return _accountRepository.GetAllAccounts();
        }

        public AccountManagementQueryResult GetAccounts(AccountManagementFilter filter)
        {
            return _accountRepository.GetAccounts(filter);
        }

        public AccountLoginResult ValidateLogin(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new AccountLoginResult
                {
                    Success = false,
                    ErrorMessage = "請輸入帳號與密碼"
                };
            }

            var user = _accountRepository.GetAccountByUserName(username);
            if (user == null)
            {
                return InvalidLogin();
            }

            if (!user.isActive)
            {
                return new AccountLoginResult
                {
                    Success = false,
                    ErrorMessage = "該帳號異常，請聯絡管理員"
                };
            }

            if (PasswordHashHelper.LooksHashed(user.password))
            {
                var result = PasswordHashHelper.VerifyPassword(user, password);
                if (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return new AccountLoginResult { Success = true, User = user };
                }

                return InvalidLogin();
            }

            if (!string.Equals(user.password, password, StringComparison.Ordinal))
            {
                return InvalidLogin();
            }

            // 舊資料若仍是明文密碼，登入成功後立即升級成雜湊。
            var upgradedHash = PasswordHashHelper.HashPassword(user, password);
            _accountRepository.UpdatePasswordByUserId(user.id, upgradedHash);
            user.password = upgradedHash;

            return new AccountLoginResult
            {
                Success = true,
                User = user
            };
        }

        public ForgotPasswordRequestResult RequestPasswordReset(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new ForgotPasswordRequestResult
                {
                    Success = false,
                    Message = "請輸入電子郵箱"
                };
            }

            try
            {
                var user = _accountRepository.GetAccountByEmail(email);
                if (user == null)
                {
                    return new ForgotPasswordRequestResult
                    {
                        Success = false,
                        Message = "找不到對應的帳號資料"
                    };
                }

                if (!user.isActive)
                {
                    return new ForgotPasswordRequestResult
                    {
                        Success = false,
                        Message = "該帳號異常，請聯絡管理員"
                    };
                }

                var verificationCode = GenerateSixDigitCode();
                var expiredAt = DateTime.Now.AddMinutes(10);

                _accountRepository.CancelActivePasswordResetCodes(user.id, email);
                _accountRepository.CreatePasswordResetCode(new PasswordResetCodeRecord
                {
                    UserId = user.id,
                    Email = email,
                    Code = verificationCode,
                    ExpiredAt = expiredAt,
                    Status = 1
                });

                var emailResult = _passwordResetEmailBridge.SendResetCodeEmail(new PasswordResetEmailRequest
                {
                    UserId = user.id,
                    Email = email,
                    UserName = user.userName,
                    VerificationCode = verificationCode,
                    ExpiredAt = expiredAt
                });

                if (!emailResult.Success)
                {
                    return new ForgotPasswordRequestResult
                    {
                        Success = false,
                        Message = emailResult.Message
                    };
                }

                return new ForgotPasswordRequestResult
                {
                    Success = true,
                    Message = emailResult.Message,
                    DebugCode = emailResult.UsedFallback ? verificationCode : string.Empty,
                    ExpiredAt = expiredAt
                };
            }
            catch (SqlException sqlEx)
            {
                return new ForgotPasswordRequestResult
                {
                    Success = false,
                    Message = $"資料庫發生錯誤：{sqlEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ForgotPasswordRequestResult
                {
                    Success = false,
                    Message = $"處理忘記密碼時發生錯誤：{ex.Message}"
                };
            }
        }

        public ResetPasswordResult ResetPassword(string email, string code, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = "請確認電子郵箱、驗證碼與新密碼都已填寫"
                };
            }

            try
            {
                var resetCode = _accountRepository.GetLatestPasswordResetCode(email, code);
                if (resetCode == null)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "驗證碼錯誤或找不到對應紀錄"
                    };
                }

                if (resetCode.Status == 2)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已使用過"
                    };
                }

                if (resetCode.Status == 3)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已過期"
                    };
                }

                if (resetCode.Status == 4)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已失效，請重新申請"
                    };
                }

                if (resetCode.ExpiredAt < DateTime.Now)
                {
                    _accountRepository.MarkPasswordResetCodeAsExpired(resetCode.Id);
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "驗證碼已超過 10 分鐘有效期，請重新申請"
                    };
                }

                var passwordHolder = new account { id = resetCode.UserId };
                var hashedPassword = PasswordHashHelper.HashPassword(passwordHolder, newPassword);

                _accountRepository.UpdatePasswordByUserId(resetCode.UserId, hashedPassword);
                _accountRepository.MarkPasswordResetCodeAsVerified(resetCode.Id);
                _accountRepository.CancelActivePasswordResetCodes(resetCode.UserId, email);

                return new ResetPasswordResult
                {
                    Success = true,
                    Message = "密碼已重設完成，請使用新密碼重新登入"
                };
            }
            catch (SqlException sqlEx)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = $"重設密碼時發生資料庫錯誤：{sqlEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = $"重設密碼時發生錯誤：{ex.Message}"
                };
            }
        }

        public ApiDetailedOperationResponse UpdateAccount(account user)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(user.password))
                {
                    user.password = PasswordHashHelper.HashPassword(user, user.password);
                }

                _accountRepository.UpdateAccount(user);
                return new ApiDetailedOperationResponse
                {
                    Success = true,
                    Message = "帳號更新成功"
                };
            }
            catch (SqlException sqlEx)
            {
                return new ApiDetailedOperationResponse
                {
                    Success = false,
                    Message = $"帳號更新失敗：{sqlEx.Message}",
                    ErrorCode = sqlEx.Number
                };
            }
            catch (Exception ex)
            {
                return new ApiDetailedOperationResponse
                {
                    Success = false,
                    Message = $"帳號更新失敗：{ex.Message}",
                    StackTrace = ex.StackTrace
                };
            }
        }

        public ApiOperationResponse ToggleAccountStatus(int id, bool isActive)
        {
            try
            {
                _accountRepository.ToggleAccountStatus(id, isActive);
                return new ApiOperationResponse
                {
                    Success = true,
                    Message = isActive ? "帳號已啟用" : "帳號已停用"
                };
            }
            catch (Exception ex)
            {
                return new ApiOperationResponse
                {
                    Success = false,
                    Message = $"更新帳號狀態失敗：{ex.Message}"
                };
            }
        }

        public bool CreateAccount(account user)
        {
            try
            {
                user.role = string.IsNullOrWhiteSpace(user.role) ? "user" : user.role;
                user.isActive = true;
                user.password = PasswordHashHelper.HashPassword(user, user.password);
                _accountRepository.CreateAccount(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public PasswordResetEmailDiagnosticInfo GetPasswordResetEmailDiagnosticInfo()
        {
            var localSecretsPath = Path.Combine(_environment.ContentRootPath, "appsettings.LocalSecrets.json");
            var scriptPath = Path.Combine(
                _environment.ContentRootPath,
                _passwordResetEmailOptions.ScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));

            return new PasswordResetEmailDiagnosticInfo
            {
                EnablePythonBridge = _passwordResetEmailOptions.EnablePythonBridge,
                FallbackToLogWhenUnavailable = _passwordResetEmailOptions.FallbackToLogWhenUnavailable,
                PythonExecutable = _passwordResetEmailOptions.PythonExecutable,
                ScriptRelativePath = _passwordResetEmailOptions.ScriptRelativePath,
                ScriptExists = File.Exists(scriptPath),
                LocalSecretsExists = File.Exists(localSecretsPath),
                SenderEmail = _passwordResetEmailOptions.SenderEmail,
                SenderPasswordLength = _passwordResetEmailOptions.SenderPassword?.Length ?? 0,
                SmtpHost = _passwordResetEmailOptions.SmtpHost,
                SmtpPort = _passwordResetEmailOptions.SmtpPort
            };
        }

        private static string GenerateSixDigitCode()
        {
            return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }

        private static AccountLoginResult InvalidLogin()
        {
            return new AccountLoginResult
            {
                Success = false,
                ErrorMessage = "帳號或密碼錯誤"
            };
        }
    }
}
