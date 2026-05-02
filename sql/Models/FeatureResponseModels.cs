namespace sql.Models
{
    // 這份檔案集中放「功能層回應模型」。
    // 它們通常是 Controller / Service / Repository 之間共用的資料形狀，
    // 目的是讓前後端看到的欄位更穩定，而不是每次都臨時拼匿名物件。
    public class UserReservationsResponse
    {
        public List<ScheduledReservationItem> ScheduledReservations { get; set; } = new();
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
        public List<HistoryReservationItem> HistoryReservations { get; set; } = new();
        public string ServerTaiwanTime { get; set; } = string.Empty;
    }

    // 未來預約資料除了基本時段與狀態，第二階段開始也會帶出風險摘要，
    // 讓前台與後台都能直接知道這筆預約是正常保留，還是預估到時仍需排隊。
    public class ScheduledReservationItem
    {
        public int Id { get; set; }
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ReservationTime { get; set; }
        public DateTime ReservedStartTime { get; set; }
        public DateTime ReservedEndTime { get; set; }
        public int DurationMinutes { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusCssClass { get; set; } = string.Empty;
        public bool QueueExpected { get; set; }
        public int ReservedCapacityCount { get; set; }
        public int ForecastWaitingCount { get; set; }
        public string RiskSummary { get; set; } = string.Empty;
    }

    // 設備可用性檢查結果。
    // 這類模型的重點是讓前端一次拿到目前是否可預約、是否已滿、
    // 以及相關的開放時間與等待估算基礎資料。
    public class EquipmentAvailabilityResponse
    {
        public bool IsAvailable { get; set; }
        public bool CanReserve { get; set; }
        public bool IsFull { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CurrentUsers { get; set; }
        public int MaxUsers { get; set; }
        public int AverageUsageTime { get; set; }
        public string ServerTaiwanTime { get; set; } = string.Empty;
        public string OpenTime { get; set; } = string.Empty;
        public string CloseTime { get; set; } = string.Empty;
    }

    public class QueueInfoResponse
    {
        public int WaitingCount { get; set; }
        public int CurrentUsers { get; set; }
        public int MaxUsers { get; set; }
        public List<QueueListItem> QueueList { get; set; } = new();
    }

    public class QueueListItem
    {
        public string UserId { get; set; } = string.Empty;
        public int Position { get; set; }
        public DateTime QueueTime { get; set; }
    }

    public class QueueDebugInfoResponse
    {
        public string Equipment { get; set; } = string.Empty;
        public int CurrentUsers { get; set; }
        public int MaxUsers { get; set; }
        public int QueueCount { get; set; }
        public bool HasVacancy { get; set; }
        public List<QueueWaitingUser> WaitingUsers { get; set; } = new();
    }

    public class QueueWaitingUser
    {
        public string UserId { get; set; } = string.Empty;
        public int Position { get; set; }
    }

    // 後台總覽頁使用的彙整模型。
    public class ReservationDashboardResponse
    {
        public List<ScheduledReservationItem> ScheduledReservations { get; set; } = new();
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
        public string ServerTaiwanTime { get; set; } = string.Empty;
    }

    // 單設備預約鏈，讓管理者把同一台設備的未來預約、使用中與排隊中一次看完。
    public class EquipmentReservationChainResponse
    {
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public List<ScheduledReservationItem> ScheduledReservations { get; set; } = new();
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
    }
}
