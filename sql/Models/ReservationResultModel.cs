using Microsoft.AspNetCore.Mvc;

namespace sql.Models
{
    // 預約結果模型
    public class ReservationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? WaitingPosition { get; set; } // 排隊位置
        public int? EstimatedWaitTime { get; set; } // 預計等待時間（分鐘）
        public DateTime? ExpectedStartTime { get; set; } // 預計開始時間
    }
}
