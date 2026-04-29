using Microsoft.Extensions.Options;
using sql.Models;
using System.Diagnostics;
using System.Text.Json;

namespace sql.Services
{
    // PasswordResetEmailBridge 是 C# 與未來 Python 發信流程之間的橋接點。
    // 預設先走安全 fallback：寫本機 log。
    // 當設定開啟且寄件參數齊全時，再改成呼叫 Python 腳本。
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
                    "目前未啟用 Python 寄信橋接，已改寫入測試紀錄。");
            }
            catch (Exception ex)
            {
                if (_options.FallbackToLogWhenUnavailable)
                {
                    return WriteFallbackLog(
                        request,
                        $"Python 寄信橋接失敗，已改寫入測試紀錄。原因：{ex.Message}");
                }

                return new PasswordResetEmailSendResult
                {
                    Success = false,
                    Message = $"寄信橋接失敗: {ex.Message}"
                };
            }
        }

        // 測試環境先把寄信請求寫進本機 log。
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

        // 正式接軌點：透過 Python 腳本處理 SMTP 發信。
        private PasswordResetEmailSendResult TrySendWithPython(PasswordResetEmailRequest request)
        {
            var scriptPath = Path.Combine(
                _environment.ContentRootPath,
                _options.ScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"找不到 Python 寄信腳本: {scriptPath}");
            }

            if (string.IsNullOrWhiteSpace(_options.SenderEmail) ||
                string.IsNullOrWhiteSpace(_options.SenderPassword))
            {
                throw new InvalidOperationException("尚未設定寄信帳號或密碼");
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
                    throw new InvalidOperationException("無法啟動 Python 寄信程序");
                }

                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Python 寄信程序失敗。ExitCode={process.ExitCode}，Error={standardError}");
                }

                return new PasswordResetEmailSendResult
                {
                    Success = true,
                    Message = "驗證碼已交由 Python 寄信腳本處理",
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
    }
}
