namespace sql.Models
{
    // 管理者在真正送出調整前，先看這次變更會造成什麼影響。
    // 這個模型不負責真正寫資料，只是把推算結果整理給後台畫面。
    public class ReservationAdjustmentPreviewResponse
    {
        public bool CanReschedule { get; set; }
        public bool QueueExpected { get; set; }
        public int ReservedCapacityCount { get; set; }
        public int ForecastWaitingCount { get; set; }
        public string TargetStatusText { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ScheduledStartTimeText { get; set; } = string.Empty;
        public string ScheduledEndTimeText { get; set; } = string.Empty;
    }
}
