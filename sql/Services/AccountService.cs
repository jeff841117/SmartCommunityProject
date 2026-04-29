using Microsoft.Data.SqlClient;
using sql.Models;
using sql.Repositories;
using System.Security.Cryptography;

namespace sql.Services
{
    // AccountService 負責帳號模組的流程協調。
    // Repository 專心做資料存取，Service 則補上欄位驗證、錯誤轉譯與流程判斷。
    public class AccountService
    {
        private readonly AccountRepository _accountRepository;
        private readonly PasswordResetEmailBridge _passwordResetEmailBridge;

        public AccountService(AccountRepository accountRepository, PasswordResetEmailBridge passwordResetEmailBridge)
        {
            _accountRepository = accountRepository;
            _passwordResetEmailBridge = passwordResetEmailBridge;
        }

        public List<account> GetAllAccounts()
        {
            return _accountRepository.GetAllAccounts();
        }

        public account? ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
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
                    Message = "請輸入電子郵件"
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
                        Message = "查無對應的電子郵件"
                    };
                }

                var verificationCode = GenerateSixDigitCode();
                var expiredAt = DateTime.Now.AddMinutes(10);

                // 同一個使用者重新申請時，先把舊的有效驗證碼標成已取消，
                // 避免同時間存在多組還能使用的驗證碼。
                _accountRepository.CancelActivePasswordResetCodes(user.id, email);
                _accountRepository.CreatePasswordResetCode(new PasswordResetCodeRecord
                {
                    UserId = user.id,
                    Email = email,
                    Code = verificationCode,
                    ExpiredAt = expiredAt,
                    Status = 1
                });

                // 這裡先不直接綁死 Python 指令，而是先交給 bridge。
                // 後面若要改成真正發信，只需要替換 bridge 內部實作。
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
                    Message = "驗證碼已建立，且已送進寄信橋接流程。正式環境後續會改由 Python 寄送 Email。",
                    DebugCode = verificationCode,
                    ExpiredAt = expiredAt
                };
            }
            catch (SqlException sqlEx)
            {
                return new ForgotPasswordRequestResult
                {
                    Success = false,
                    Message = $"忘記密碼資料庫處理失敗: {sqlEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ForgotPasswordRequestResult
                {
                    Success = false,
                    Message = $"忘記密碼流程失敗: {ex.Message}"
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
                    Message = "請完整輸入電子郵件、驗證碼與新密碼"
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
                        Message = "驗證碼錯誤或不存在"
                    };
                }

                if (resetCode.Status == 2)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已經使用過了"
                    };
                }

                if (resetCode.Status == 3)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已經過期"
                    };
                }

                if (resetCode.Status == 4)
                {
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "這組驗證碼已被取消，請重新申請"
                    };
                }

                if (resetCode.ExpiredAt < DateTime.Now)
                {
                    _accountRepository.MarkPasswordResetCodeAsExpired(resetCode.Id);
                    return new ResetPasswordResult
                    {
                        Success = false,
                        Message = "驗證碼已超過 10 分鐘有效時間，請重新申請"
                    };
                }

                _accountRepository.UpdatePasswordByUserId(resetCode.UserId, newPassword);
                _accountRepository.MarkPasswordResetCodeAsVerified(resetCode.Id);
                _accountRepository.CancelActivePasswordResetCodes(resetCode.UserId, email);

                return new ResetPasswordResult
                {
                    Success = true,
                    Message = "密碼已更新，請使用新密碼登入"
                };
            }
            catch (SqlException sqlEx)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = $"重設密碼資料庫處理失敗: {sqlEx.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = $"重設密碼流程失敗: {ex.Message}"
                };
            }
        }

        public ApiDetailedOperationResponse UpdateAccount(account user)
        {
            // 帳號更新時，密碼目前仍視為必要欄位。
            if (string.IsNullOrEmpty(user.password))
            {
                return new ApiDetailedOperationResponse
                {
                    Success = false,
                    Message = "請輸入密碼"
                };
            }

            try
            {
                _accountRepository.UpdateAccount(user);
                return new ApiDetailedOperationResponse
                {
                    Success = true,
                    Message = "更新成功"
                };
            }
            catch (SqlException sqlEx)
            {
                return new ApiDetailedOperationResponse
                {
                    Success = false,
                    Message = $"資料庫錯誤: {sqlEx.Message}",
                    ErrorCode = sqlEx.Number
                };
            }
            catch (Exception ex)
            {
                return new ApiDetailedOperationResponse
                {
                    Success = false,
                    Message = $"更新失敗: {ex.Message}",
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

        // 驗證碼使用固定 6 碼數字，方便後面和 Email 找回密碼流程接軌。
        private static string GenerateSixDigitCode()
        {
            return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }
    }
}
