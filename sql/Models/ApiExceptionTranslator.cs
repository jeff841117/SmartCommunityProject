namespace sql.Models
{
    // 這個類別專門把例外訊息翻成比較適合給前端或使用者看的文字。
    // 新手可以把它理解成「技術錯誤 -> 使用者語言」的轉接器。
    public static class ApiExceptionTranslator
    {
        public static string ToUserMessage(Exception exception, string defaultMessage = "系統發生錯誤，請聯繫管理員")
        {
            if (exception.Message.Contains("開放時間"))
            {
                return exception.Message;
            }

            if (exception.Message.Contains("連接") || exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                return "系統暫時無法處理您的請求，請稍後再試";
            }

            if (exception.Message.Contains("超時") || exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return "請求超時，請檢查網路連接";
            }

            return defaultMessage;
        }
    }
}
