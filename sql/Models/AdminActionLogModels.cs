namespace sql.Models
{
    // 管理者操作紀錄模型。
    // 目前先聚焦在後台干預預約與排隊流程的幾個核心動作，
    // 之後若還有設備管理、會員管理等操作，也能沿用這套結構往下擴充。
    public class AdminActionLogEntry
    {
        public int AdminUserId { get; set; }
        public int ActionType { get; set; }
        public int TargetType { get; set; }
        public int TargetId { get; set; }
        public string? Reason { get; set; }
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
}
