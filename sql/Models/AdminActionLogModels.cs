namespace sql.Models
{
    // 管理者操作紀錄的基本寫入模型。
    // 這一層先專注在把資料寫進資料庫，後面若要延伸查詢欄位，再另外補展示模型。
    public class AdminActionLogEntry
    {
        public int AdminUserId { get; set; }
        public int ActionType { get; set; }
        public int TargetType { get; set; }
        public int TargetId { get; set; }
        public string? Reason { get; set; }
    }

    // 管理者操作紀錄展示模型。
    // 後台查詢頁直接使用這個模型，避免 View 自己再去猜數字代表什麼。
    public class AdminActionLogListItem
    {
        public int Id { get; set; }
        public int AdminUserId { get; set; }
        public string AdminUserName { get; set; } = string.Empty;
        public int ActionType { get; set; }
        public string ActionTypeText { get; set; } = string.Empty;
        public int TargetType { get; set; }
        public string TargetTypeText { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum AdminActionType
    {
        ForceEndUsage = 1,
        ForceCancelScheduledReservation = 2,
        ForceRescheduleScheduledReservation = 3,
        ForceCancelQueue = 4
    }

    public enum AdminActionTargetType
    {
        Reservation = 1,
        WaitingQueue = 2
    }

    public static class AdminActionLogDisplayHelper
    {
        public static string GetActionTypeText(int actionType)
        {
            return actionType switch
            {
                (int)AdminActionType.ForceEndUsage => "強制結束使用",
                (int)AdminActionType.ForceCancelScheduledReservation => "取消未來預約",
                (int)AdminActionType.ForceRescheduleScheduledReservation => "調整未來預約時段",
                (int)AdminActionType.ForceCancelQueue => "移除排隊紀錄",
                _ => "未知操作"
            };
        }

        public static string GetTargetTypeText(int targetType)
        {
            return targetType switch
            {
                (int)AdminActionTargetType.Reservation => "預約",
                (int)AdminActionTargetType.WaitingQueue => "排隊",
                _ => "未知目標"
            };
        }
    }
}
