using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    
    public class Equipment  // 設備
    {
        public byte Id { get; set; }
        [Required]
        [StringLength(100)]
        public string equipmentName { get; set; } = string.Empty;  // 設備名稱
        [Range(1, 100)]
        public byte MaxUsers { get; set; } // 同時間設備使用上限
        [Range(1, 1440)]
        public short  AvailableTime { get; set; } // 可使用時間
        public TimeSpan OpenTime { get; set; } // 開放時間
        public TimeSpan CloseTime { get; set; } // 關閉時間
        public string EquipmentCategory { get; set; } = "場館"; // 設備種類
    }

}
