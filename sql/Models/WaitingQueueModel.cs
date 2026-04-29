using Microsoft.AspNetCore.Mvc;

namespace sql.Models
{
    // 排隊記錄模型
    public class WaitingQueue
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; } // 設備ID
        public string UserId { get; set; } = string.Empty; // 使用者ID
        public DateTime QueueTime { get; set; } // 加入排隊時間
        public int Position { get; set; } // 排隊位置
    }
}
