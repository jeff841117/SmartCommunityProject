using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 建立未來時段預約時，前端會送這個模型進來。
    // ConfirmQueueExpected 的用途是：
    // 如果系統先推算出「到時候大概率仍然要排隊」，
    // 會先要求使用者再確認一次，避免誤以為這是保證可直接使用的預約。
    public class FutureReservationRequestViewModel
    {
        [Required(ErrorMessage = "請選擇設備")]
        public byte EquipmentId { get; set; }

        [Required(ErrorMessage = "請選擇預約日期")]
        public string ReservationDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇預約時段")]
        public string SelectedSlotStartTime { get; set; } = string.Empty;

        public bool ConfirmQueueExpected { get; set; }
    }

    // 單一時段的規劃結果。
    // 這裡會把「是否可選」「是否需要再次確認」「目前保留名額數」
    // 和「推算到該時段時前面可能還有幾人在等」一起交給前端。
    public class FutureReservationSlotItem
    {
        public string SlotStartTime { get; set; } = string.Empty;
        public string SlotEndTime { get; set; } = string.Empty;
        public string DisplayLabel { get; set; } = string.Empty;
        public bool IsSelectable { get; set; }
        public bool RequiresQueueConfirmation { get; set; }
        public string StatusNote { get; set; } = string.Empty;
        public int ReservedCapacityCount { get; set; }
        public int ForecastWaitingCount { get; set; }
    }

    // 預約頁載入未來時段時的整包回應。
    public class FutureReservationPlanningResponse
    {
        public byte EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string ReservationDate { get; set; } = string.Empty;
        public int SlotIntervalMinutes { get; set; }
        public int AvailableTimeMinutes { get; set; }
        public string OpenTime { get; set; } = string.Empty;
        public string CloseTime { get; set; } = string.Empty;
        public string PlanningNote { get; set; } = string.Empty;
        public List<FutureReservationSlotItem> Slots { get; set; } = new();
    }

    // 這是後端內部用來判斷某個未來時段的推算結果。
    // HasReservedCapacityConflict = 該時段保留名額已滿，不能再選。
    // QueueExpected = 雖然還能預約，但依目前隊列推算，到時仍可能要排隊。
    public class FutureReservationForecast
    {
        public bool HasReservedCapacityConflict { get; set; }
        public bool QueueExpected { get; set; }
        public int ReservedCapacityCount { get; set; }
        public int ForecastWaitingCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
