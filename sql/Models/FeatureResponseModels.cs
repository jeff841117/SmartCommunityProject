namespace sql.Models
{
    // 這份檔案放的是「功能層會共用的回應資料模型」。
    // 它們不一定直接對應資料表，而是對應畫面或 API 需要的資料形狀。
    public class UserReservationsResponse
    {
        public List<ScheduledReservationItem> ScheduledReservations { get; set; } = new();
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
        public List<HistoryReservationItem> HistoryReservations { get; set; } = new();
        public string ServerTaiwanTime { get; set; } = string.Empty;
    }

    // 未來預約清單專用。
    // 和進行中預約分開，前端比較容易決定顯示什麼按鈕。
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
    }

    // 預約頁在點「立即預約」前，會先用這份資料判斷：
    // 1. 現在能不能預約
    // 2. 設備有沒有滿
    // 3. 目前畫面要顯示什麼提示
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

    // 管理者總覽頁會用到這份資料，讓後台一次看到目前的預約主狀態。
    public class ReservationDashboardResponse
    {
        public List<ScheduledReservationItem> ScheduledReservations { get; set; } = new();
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
        public string ServerTaiwanTime { get; set; } = string.Empty;
    }
}
