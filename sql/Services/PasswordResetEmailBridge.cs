using Microsoft.Extensions.Options;
using sql.Models;
using System.Diagnostics;
using System.Text.Json;

namespace sql.Services
{
    // PasswordResetEmailBridge 負責把 C# 的忘記密碼流程接到 Python 寄信腳本。
    // 如果 Python / SMTP 有問題，這裡會回退到測試記錄檔，避免整個忘記密碼流程直接中斷。
    public class PasswordResetEmailBridge
    {
        private readonly IWebHostEnvironment _environment;
        private readonly PasswordResetEmailOptions _options;

        public PasswordResetEmailBridge(
            IWebHostEnvironment environment,
            IOptions<PasswordResetEmailOptions> options)
        {
            _environment = environment;
            _options = options.Value;
        }

        public PasswordResetEmailSendResult SendResetCodeEmail(PasswordResetEmailRequest request)
        {
            try
            {
                if (_options.EnablePythonBridge)
                {
                    return TrySendWithPython(request);
                }

                return WriteFallbackLog(
                    request,
                    "目前未啟用 Python 寄信，已改記錄到測試橋接檔案。");
            }
            catch (Exception ex)
            {
                WriteErrorLog(request, ex);

                if (_options.FallbackToLogWhenUnavailable)
                {
                    return WriteFallbackLog(
                        request,
                        $"Python / SMTP 寄信失敗，已改記錄到測試橋接檔案。原因：{ex.Message}");
                }

                return new PasswordResetEmailSendResult
                {
                    Success = false,
                    Message = $"寄信失敗：{ex.Message}"
                };
            }
        }

        // 測試橋接模式：不真的寄信，只把這次寄信需求寫入 RuntimeLogs。
        private PasswordResetEmailSendResult WriteFallbackLog(
            PasswordResetEmailRequest request,
            string message)
        {
            var logDirectory = Path.Combine(_environment.ContentRootPath, "RuntimeLogs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "password-reset-email-requests.jsonl");
            var payload = new
            {
                SentAt = DateTime.Now,
                request.UserId,
                request.UserName,
                request.Email,
                request.VerificationCode,
                request.ExpiredAt,
                Mode = "TestBridge"
            };

            var jsonLine = JsonSerializer.Serialize(payload);
            File.AppendAllText(logPath, jsonLine + Environment.NewLine);

            return new PasswordResetEmailSendResult
            {
                Success = true,
                Message = message,
                DebugOutputPath = logPath,
                UsedFallback = true
            };
        }

        // 真實模式：呼叫 Python 寄信腳本，由它負責 SMTP 連線與送信。
        private PasswordResetEmailSendResult TrySendWithPython(PasswordResetEmailRequest request)
        {
            var scriptPath = Path.Combine(
                _environment.ContentRootPath,
                _options.ScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"找不到 Python 寄信腳本：{scriptPath}");
            }

            if (string.IsNullOrWhiteSpace(_options.SenderEmail) ||
                string.IsNullOrWhiteSpace(_options.SenderPassword))
            {
                throw new InvalidOperationException("未設定寄信帳號或應用程式密碼。");
            }

            var runtimeDirectory = Path.Combine(_environment.ContentRootPath, "RuntimeLogs");
            Directory.CreateDirectory(runtimeDirectory);

            var payloadPath = Path.Combine(runtimeDirectory, $"password-reset-payload-{Guid.NewGuid():N}.json");
            File.WriteAllText(payloadPath, JsonSerializer.Serialize(request));

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _options.PythonExecutable,
                    Arguments = $"\"{scriptPath}\" \"{payloadPath}\"",
                    WorkingDirectory = _environment.ContentRootPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.Environment["PASSWORD_RESET_SENDER_EMAIL"] = _options.SenderEmail;
                startInfo.Environment["PASSWORD_RESET_SENDER_PASSWORD"] = _options.SenderPassword;
                startInfo.Environment["PASSWORD_RESET_SMTP_HOST"] = _options.SmtpHost;
                startInfo.Environment["PASSWORD_RESET_SMTP_PORT"] = _options.SmtpPort.ToString();

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("無法啟動 Python 寄信程序。");
                }

                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Python 寄信程序失敗，ExitCode={process.ExitCode}，Error={standardError}");
                }

                return new PasswordResetEmailSendResult
                {
                    Success = true,
                    Message = "驗證碼已透過 Gmail SMTP 寄出。",
                    DebugOutputPath = standardOutput.Trim(),
                    UsedFallback = false
                };
            }
            finally
            {
                if (File.Exists(payloadPath))
                {
                    File.Delete(payloadPath);
                }
            }
        }

        // 若 Python / SMTP 失敗，這份錯誤記錄會比前端訊息更容易追查真正原因。
        private void WriteErrorLog(PasswordResetEmailRequest request, Exception exception)
        {
            var logDirectory = Path.Combine(_environment.ContentRootPath, "RuntimeLogs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "password-reset-email-errors.jsonl");
            var payload = new
            {
                LoggedAt = DateTime.Now,
                request.UserId,
                request.UserName,
                request.Email,
                request.VerificationCode,
                ExceptionType = exception.GetType().FullName,
                exception.Message,
                exception.StackTrace
            };

            var jsonLine = JsonSerializer.Serialize(payload);
            File.AppendAllText(logPath, jsonLine + Environment.NewLine);
        }
    }
}
