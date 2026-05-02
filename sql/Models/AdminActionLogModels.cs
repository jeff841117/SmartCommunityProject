namespace sql.Models
{
    // 這是寫入資料庫時使用的最小日誌模型。
    // 目的是讓 Service 層只要描述「誰做了什麼」，Repository 就能負責存入資料表。
    public class AdminActionLogEntry
    {
        public int AdminUserId { get; set; }
        public int ActionType { get; set; }
        public int TargetType { get; set; }
        public int TargetId { get; set; }
        public string? Reason { get; set; }
    }

    // 這是後台列表頁使用的顯示模型。
    // 它和寫入模型不同，會多帶顯示用文字，避免 View 自己再判斷 enum。
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

    // 篩選模型放在同一份檔案中，方便後台查詢頁與 Repository 共用。
    public class AdminActionLogFilter
    {
        public string? AdminKeyword { get; set; }
        public int? ActionType { get; set; }
        public int? TargetType { get; set; }
        public string? Keyword { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Take { get; set; } = 100;
    }

    // 這個小模型是為了讓下拉選單用同一種資料格式。
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
