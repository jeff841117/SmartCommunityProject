using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 預約記錄模型
    public class Reservation
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; } // 設備ID
        public string UserId { get; set; } = string.Empty; // 使用者ID
        public DateTime StartTime { get; set; } // 開始使用時間
        public DateTime? EndTime { get; set; } // 結束使用時間（可為空表示還在進行中）
        public DateTime ReservationTime { get; set; } // 預約時間
        public ReservationStatus Status { get; set; } // 預約狀態
        public DateTime? ReservedStartTime { get; set; } // 預定開始時間（未來時段預約用）
        public DateTime? ReservedEndTime { get; set; } // 預定結束時間
        public int? DurationMinutes { get; set; } // 預約時長
        public int? ReservationType { get; set; } // 預約類型：即時 / 未來時段
    }

    // 預約狀態枚舉
    public enum ReservationStatus
    {
        Waiting = 0,    // 等待中（排隊）
        InProgress = 1, // 使用中
        Completed = 2,  // 已完成
        Cancelled = 3,  // 已取消
        Scheduled = 4   // 已預約但尚未開始
    }
}
