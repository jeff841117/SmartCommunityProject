using Microsoft.AspNetCore.Mvc;

namespace sql.Models
{
    // 排隊記錄模型
    public class WaitingQueue
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; } // 設備ID
        public string UserId { get; set; } = string.Empty; // 目前相容舊資料，實際存的是帳號字串識別值
        public DateTime QueueTime { get; set; } // 加入排隊時間
        public int Position { get; set; } // 排隊位置
        public int? ReservationId { get; set; } // 如果是預約到點轉排隊，會指向原本的預約記錄
        public int QueueType { get; set; } // 1 = 一般即時排隊，2 = 預約到點後轉排隊
        public int QueueStatus { get; set; } // 1 = 排隊中，2 = 已轉為使用中，3 = 已取消
    }
}
