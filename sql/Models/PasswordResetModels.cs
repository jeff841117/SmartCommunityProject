namespace sql.Models
{
    // PasswordResetCodeRecord 對應 PasswordResetCodes 資料表的核心欄位。
    public class PasswordResetCodeRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public int Status { get; set; }
    }

    // 申請驗證碼這一步的流程結果。
    public class ForgotPasswordRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DebugCode { get; set; } = string.Empty;
        public DateTime? ExpiredAt { get; set; }
    }

    // 驗證碼核對與重設密碼這一步的流程結果。
    public class ResetPasswordResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // 這是 C# 端要交給寄信橋接層的資料。
    // 後面如果改成真正呼叫 Python 腳本，這個模型就能當成固定契約。
    public class PasswordResetEmailRequest
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
    }

    // 寄信橋接層回傳的結果。
    public class PasswordResetEmailSendResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DebugOutputPath { get; set; } = string.Empty;
        public bool UsedFallback { get; set; }
    }

    // 這是忘記密碼寄信橋接的設定模型。
    public class PasswordResetEmailOptions
    {
        public bool EnablePythonBridge { get; set; }
        public bool FallbackToLogWhenUnavailable { get; set; } = true;
        public string PythonExecutable { get; set; } = "python";
        public string ScriptRelativePath { get; set; } = "Tools/password_reset_email_sender.py";
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
    }
}
