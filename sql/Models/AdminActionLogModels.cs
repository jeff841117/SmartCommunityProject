namespace sql.Models
{
    // 這是寫入資料庫用的管理者操作紀錄模型。
    public class AdminActionLogEntry
    {
        public int AdminUserId { get; set; }
        public int ActionType { get; set; }
        public int TargetType { get; set; }
        public int TargetId { get; set; }
        public string? Reason { get; set; }
    }

    // 這是列表頁顯示用的模型，已經把型別轉成中文文字。
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

    // 操作紀錄查詢頁用的篩選條件。
    public class AdminActionLogFilter
    {
        public string? AdminKeyword { get; set; }
        public int? ActionType { get; set; }
        public int? TargetType { get; set; }
        public string? Keyword { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // 查詢結果除了資料列表，還包含分頁資訊。
    public class AdminActionLogQueryResult
    {
        public List<AdminActionLogListItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => PageSize <= 0
            ? 1
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class AdminActionFilterOption
    {
        public int Value { get; set; }
        public string Text { get; set; } = string.Empty;
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

        public static List<AdminActionFilterOption> GetActionTypeOptions()
        {
            return
            [
                new AdminActionFilterOption
                {
                    Value = (int)AdminActionType.ForceEndUsage,
                    Text = GetActionTypeText((int)AdminActionType.ForceEndUsage)
                },
                new AdminActionFilterOption
                {
                    Value = (int)AdminActionType.ForceCancelScheduledReservation,
                    Text = GetActionTypeText((int)AdminActionType.ForceCancelScheduledReservation)
                },
                new AdminActionFilterOption
                {
                    Value = (int)AdminActionType.ForceRescheduleScheduledReservation,
                    Text = GetActionTypeText((int)AdminActionType.ForceRescheduleScheduledReservation)
                },
                new AdminActionFilterOption
                {
                    Value = (int)AdminActionType.ForceCancelQueue,
                    Text = GetActionTypeText((int)AdminActionType.ForceCancelQueue)
                }
            ];
        }

        public static List<AdminActionFilterOption> GetTargetTypeOptions()
        {
            return
            [
                new AdminActionFilterOption
                {
                    Value = (int)AdminActionTargetType.Reservation,
                    Text = GetTargetTypeText((int)AdminActionTargetType.Reservation)
                },
                new AdminActionFilterOption
                {
                    Value = (int)AdminActionTargetType.WaitingQueue,
                    Text = GetTargetTypeText((int)AdminActionTargetType.WaitingQueue)
                }
            ];
        }
    }
}
