namespace sql.Models
{
    // 我的預約頁回傳資料。
    // 這裡把未來預約、使用中、排隊中與歷史資料拆開，
    // 讓前台與 API 可以直接照區塊顯示，不需要再額外整理 Dictionary。
    public class UserReservationsResponse
    {
        public List<ScheduledReservationItem> ScheduledReservations { get; set; } = new();
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
        public List<HistoryReservationItem> HistoryReservations { get; set; } = new();
        public string ServerTaiwanTime { get; set; } = string.Empty;
    }

    // 未來預約項目。
    // 這裡除了時間與狀態，也會帶出風險摘要，
    // 方便前台與後台直接知道這筆預約是否可能到時轉排隊。
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

    // 單設備可用性檢查結果。
    // 前台立即使用、查看預約時間與排隊資訊都會用到這組資料。
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

    // 後台預約 / 排隊總覽回傳資料。
    public class ReservationDashboardResponse
    {
        public List<ScheduledReservationItem> ScheduledReservations { get; set; } = new();
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
        public List<EquipmentDashboardSummaryItem> EquipmentSummaries { get; set; } = new();
        public string ServerTaiwanTime { get; set; } = string.Empty;
    }

    // 後台設備摘要。
    // 用來快速看每台設備目前的預約壓力、排隊人數與風險等級。
    public class EquipmentDashboardSummaryItem
    {
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public int ScheduledCount { get; set; }
        public int ActiveCount { get; set; }
        public int WaitingCount { get; set; }
        public int QueueExpectedCount { get; set; }
        public int RiskyScheduledCount { get; set; }
        public string PressureLevel { get; set; } = string.Empty;
        public string PressureCssClass { get; set; } = string.Empty;
        public string PressureSummary { get; set; } = string.Empty;
    }

    // 後台總覽篩選條件。
    // 這些條件同時用在頁面與匯出，避免畫面與 CSV 規則不一致。
    public class ReservationDashboardFilter
    {
        public string? EquipmentKeyword { get; set; }
        public string? UserKeyword { get; set; }
        public int? ScheduledStatus { get; set; }
        public int? WaitingQueueType { get; set; }
        public bool RiskOnly { get; set; }
    }

    public class ReservationDashboardFilterOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public static class ReservationDashboardFilterOptions
    {
        public static List<ReservationDashboardFilterOption> GetScheduledStatusOptions()
        {
            return
            [
                new ReservationDashboardFilterOption
                {
                    Value = (int)ReservationStatus.Scheduled,
                    Text = ReservationDisplayHelper.GetStatusText((int)ReservationStatus.Scheduled)
                },
                new ReservationDashboardFilterOption
                {
                    Value = (int)ReservationStatus.ScheduledQueueExpected,
                    Text = ReservationDisplayHelper.GetStatusText((int)ReservationStatus.ScheduledQueueExpected)
                }
            ];
        }

        public static List<ReservationDashboardFilterOption> GetWaitingQueueTypeOptions()
        {
            return
            [
                new ReservationDashboardFilterOption
                {
                    Value = 1,
                    Text = ReservationDisplayHelper.GetQueueTypeText(1)
                },
                new ReservationDashboardFilterOption
                {
                    Value = 2,
                    Text = ReservationDisplayHelper.GetQueueTypeText(2)
                }
            ];
        }
    }

    // 單設備預約 / 排隊鏈資料。
    // 後台開設備鏈 modal 時，會一次顯示未來預約、使用中與排隊中的完整清單。
    public class EquipmentReservationChainResponse
    {
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public List<ScheduledReservationItem> ScheduledReservations { get; set; } = new();
        public List<ActiveReservationItem> ActiveReservations { get; set; } = new();
        public List<WaitingReservationItem> WaitingReservations { get; set; } = new();
    }
}
