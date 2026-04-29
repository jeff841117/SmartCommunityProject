namespace sql.Models
{
    // 這份模型只給本機診斷寄信設定使用，不包含真正密碼內容。
    public class PasswordResetEmailDiagnosticInfo
    {
        public bool EnablePythonBridge { get; set; }
        public bool FallbackToLogWhenUnavailable { get; set; }
        public string PythonExecutable { get; set; } = string.Empty;
        public string ScriptRelativePath { get; set; } = string.Empty;
        public bool ScriptExists { get; set; }
        public bool LocalSecretsExists { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public int SenderPasswordLength { get; set; }
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
    }
}
