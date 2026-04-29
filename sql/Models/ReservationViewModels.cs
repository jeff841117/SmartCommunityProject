namespace sql.Models
{
    // 這幾個 ViewModel / DTO 是專門給「我的預約」頁面使用的資料形狀。
    // 它們的目的不是取代資料表模型，而是讓前端拿到的資料欄位更清楚、
    // 也讓 Service / Repository 不需要再用 Dictionary<string, object> 傳來傳去。

    // 進行中的預約資料。
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
    }

    // 排隊中的資料。
    public class WaitingReservationItem
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime QueueTime { get; set; }
        public int Position { get; set; }
        public int AverageUsageTime { get; set; }
    }

    // 歷史記錄資料。
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
    }
}
