namespace SmartCommunity.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // 可選：讓其他控制器丟訊息到錯誤頁
        public string? Message { get; set; }
    }
}