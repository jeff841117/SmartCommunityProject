using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using sql.Models;
using sql.Repositories;
using System.Security.Cryptography;

namespace sql.Services
{
    // AccountService 負責帳號登入、忘記密碼、帳號更新等業務流程。
    // 這次把忘記密碼寄信也統一收斂在這裡，方便之後接正式 Email 與安全規則。
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

        public account? ValidateUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            return _accountRepository.ValidateUser(username, password);
        }

        public ForgotPasswordRequestResult RequestPasswordReset(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new ForgotPasswordRequestResult
                {
                    Success = false,
                    Message = "請輸入註冊電子郵件。"
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
                        Message = "查無符合的電子郵件。"
                    };
                }

                var verificationCode = GenerateSixDigitCode();
                var expiredAt = DateTime.Now.AddMinutes(10);

                // 同一個使用者若再次申請，就先把舊的有效驗證碼作廢。
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
                    Message = $"忘記密碼資料庫處理失敗：{sqlEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ForgotPasswordRequestResult
                {
                    Success = false,
                    Message = $"忘記密碼處理失敗：{ex.Message}"
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
                    Message = "請完整輸入電子郵件、驗證碼與新密碼。"
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
                        Message = "驗證碼錯誤或不存在。"
                    };
                }

                if (resetCode.Status == 2)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已經使用過。"
                    };
                }

                if (resetCode.Status == 3)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已經過期。"
                    };
                }

                if (resetCode.Status == 4)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已被新的申請取代。"
                    };
                }

                if (resetCode.ExpiredAt < DateTime.Now)
                {
                    _accountRepository.MarkPasswordResetCodeAsExpired(resetCode.Id);
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "驗證碼已超過 10 分鐘有效期限，請重新申請。"
                    };
                }

                _accountRepository.UpdatePasswordByUserId(resetCode.UserId, newPassword);
                _accountRepository.MarkPasswordResetCodeAsVerified(resetCode.Id);
                _accountRepository.CancelActivePasswordResetCodes(resetCode.UserId, email);

                return new ResetPasswordResult
                {
                    Success = true,
                    Message = "密碼重設成功，請使用新密碼登入。"
                };
            }
            catch (SqlException sqlEx)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = $"重設密碼資料庫處理失敗：{sqlEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = $"重設密碼失敗：{ex.Message}"
                };
            }
        }

        public ApiDetailedOperationResponse UpdateAccount(account user)
        {
            if (string.IsNullOrWhiteSpace(user.password))
            {
                return new ApiDetailedOperationResponse
                {
                    Success = false,
                    Message = "請輸入密碼。"
                };
            }

            try
            {
                _accountRepository.UpdateAccount(user);
                return new ApiDetailedOperationResponse
                {
                    Success = true,
                    Message = "更新成功。"
                };
            }
            catch (SqlException sqlEx)
            {
                return new ApiDetailedOperationResponse
                {
                    Success = false,
                    Message = $"資料庫處理失敗：{sqlEx.Message}",
                    ErrorCode = sqlEx.Number
                };
            }
            catch (Exception ex)
            {
                return new ApiDetailedOperationResponse
                {
                    Success = false,
                    Message = $"更新失敗：{ex.Message}",
                    StackTrace = ex.StackTrace
                };
            }
        }

        public bool CreateAccount(account user)
        {
            try
            {
                _accountRepository.CreateAccount(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 本機診斷用：確認 ASP.NET 執行時到底讀到了哪些寄信設定。
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

        // 驗證碼固定 6 碼，方便 Email 與使用者手動輸入。
        private static string GenerateSixDigitCode()
        {
            return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }
    }
}
