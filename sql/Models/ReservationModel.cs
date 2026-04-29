using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 預約記錄模型
    public class Reservation
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; } // 設備ID
        public string UserId { get; set; } // 使用者ID
        public DateTime StartTime { get; set; } // 開始使用時間
        public DateTime? EndTime { get; set; } // 結束使用時間（可為空表示還在進行中）
        public DateTime ReservationTime { get; set; } // 預約時間
        public ReservationStatus Status { get; set; } // 預約狀態
    }

    // 預約狀態枚舉
    public enum ReservationStatus
    {
        Waiting,    // 等待中（排隊）
        InProgress, // 使用中
        Completed,  // 已完成
        Cancelled   // 已取消
    }
}
