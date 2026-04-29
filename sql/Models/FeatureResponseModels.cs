namespace sql.Models
{
    // 這份檔案集中放「設備 / 預約 / 排隊」模組常用的回應模型。
    // 以前這些類別散在 Service 內部，雖然能用，但不利於 API 化後的維護。
    // 集中到 Models 後，Controller、Service、前端都比較容易對齊資料形狀。

    // 「我的預約」頁面用的整包資料。
    public class UserReservationsResponse
    {
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
        public List<HistoryReservationItem> HistoryReservations { get; set; } = new();
        public string ServerTaiwanTime { get; set; } = string.Empty;
    }

    // 預約前檢查設備狀態時回傳的完整資訊。
    // 前端會用它來決定：
    // 1. 按鈕能不能按
    // 2. 顯示「可預約 / 已滿 / 非開放時間」
    // 3. 排隊等待時間怎麼估算
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

    // 提供給一般前端頁面的排隊資訊。
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

    // 提供給管理或偵錯頁面的排隊詳細資訊。
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
}
