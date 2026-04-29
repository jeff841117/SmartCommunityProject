using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 未來時段預約的建立請求。
    // 這裡只放「畫面送進來」真正需要的欄位，
    // 不直接把整張 Reservations 表塞進來，避免前端亂帶資料。
    public class FutureReservationRequestViewModel
    {
        [Required(ErrorMessage = "請選擇設備")]
        public byte EquipmentId { get; set; }

        [Required(ErrorMessage = "請選擇預約日期")]
        public string ReservationDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇預約時段")]
        public string SelectedSlotStartTime { get; set; } = string.Empty;
    }

    // 單一時段的顯示資料。
    // 前端會依照 IsSelectable 決定按鈕能不能點。
    public class FutureReservationSlotItem
    {
        public string SlotStartTime { get; set; } = string.Empty;
        public string SlotEndTime { get; set; } = string.Empty;
        public string DisplayLabel { get; set; } = string.Empty;
        public bool IsSelectable { get; set; }
        public string StatusNote { get; set; } = string.Empty;
    }

    // 預約頁面載入未來時段時，後端回傳的規劃結果。
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
