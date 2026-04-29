using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 這份模型專門服務第二階段的「未來時段預約」功能。
    // 目前先把頁面與 API 邊界整理好，
    // 之後真正接資料庫欄位與預約狀態流轉時，就能直接沿用。
    public class FutureReservationRequestViewModel
    {
        [Required(ErrorMessage = "請選擇設備")]
        public byte EquipmentId { get; set; }

        [Required(ErrorMessage = "請選擇預約日期")]
        public string ReservationDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇預約時段")]
        public string SelectedSlotStartTime { get; set; } = string.Empty;
    }

    // 單一時段的畫面顯示資料。
    // 先用明確模型表示，比直接回傳字串陣列更適合後續 API 化。
    public class FutureReservationSlotItem
    {
        public string SlotStartTime { get; set; } = string.Empty;
        public string SlotEndTime { get; set; } = string.Empty;
        public string DisplayLabel { get; set; } = string.Empty;
        public bool IsSelectable { get; set; }
        public string StatusNote { get; set; } = string.Empty;
    }

    // 預約頁點選某台設備後，前端會用這個模型渲染可選時段。
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
}
