namespace sql.Models
{
    // 使用中的預約資料。
    // UserId 目前為了相容舊資料，實際存的是帳號字串識別值，不是會員主鍵。
    public class ActiveReservationItem
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int AvailableTime { get; set; }
        public DateTime ReservationTime { get; set; }
        public int Status { get; set; }
        public int RemainingTime { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusCssClass { get; set; } = string.Empty;
    }

    // 排隊中的資料。
    // UserId 同樣為舊資料相容用的帳號字串識別值。
    public class WaitingReservationItem
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime QueueTime { get; set; }
        public int Position { get; set; }
        public int AverageUsageTime { get; set; }
        public int QueueType { get; set; }
        public string QueueTypeText { get; set; } = string.Empty;
    }

    // 歷史紀錄資料。
    // UserId 目前仍保留帳號字串識別值，以相容既有資料表內容。
    public class HistoryReservationItem
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime ReservationTime { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusCssClass { get; set; } = string.Empty;
    }
}
