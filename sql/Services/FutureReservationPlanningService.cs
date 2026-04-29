using sql.Models;

namespace sql.Services
{
    // 這個 Service 先專心負責「規劃可選時段」。
    // 真正建立 Scheduled 預約資料會交給 ReservationService / ReservationRepository。
    public class FutureReservationPlanningService
    {
        public const int DefaultSlotIntervalMinutes = 15;
        public const int DefaultAdvanceReservationDays = 7;

        private readonly EquipmentService _equipmentService;

        public FutureReservationPlanningService(EquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        public int GetSlotIntervalMinutes()
        {
            return DefaultSlotIntervalMinutes;
        }

        public int GetAdvanceReservationDays()
        {
            return DefaultAdvanceReservationDays;
        }

        public FutureReservationPlanningResponse BuildPlanning(byte equipmentId, DateOnly? reservationDate = null)
        {
            var equipment = _equipmentService.GetEquipment(equipmentId);
            if (equipment == null)
            {
                throw new InvalidOperationException("設備不存在");
            }

            var currentTaiwanTime = GetTaiwanTime();
            var targetDate = reservationDate ?? DateOnly.FromDateTime(currentTaiwanTime);
            var targetDateTime = targetDate.ToDateTime(TimeOnly.MinValue);

            var response = new FutureReservationPlanningResponse
            {
                EquipmentId = equipment.Id,
                EquipmentName = equipment.equipmentName,
                ReservationDate = targetDate.ToString("yyyy-MM-dd"),
                SlotIntervalMinutes = DefaultSlotIntervalMinutes,
                AvailableTimeMinutes = equipment.AvailableTime,
                OpenTime = equipment.OpenTime.ToString(@"hh\:mm"),
                CloseTime = equipment.CloseTime.ToString(@"hh\:mm"),
                PlanningNote = "目前已開放未來時段預約建立。若預約時段到達時設備仍無空位，後續將依規則轉入排隊。"
            };

            var openDateTime = targetDateTime.Add(equipment.OpenTime);
            var closeDateTime = targetDateTime.Add(equipment.CloseTime);
            var latestStartTime = closeDateTime.AddMinutes(-equipment.AvailableTime);

            for (var slotTime = openDateTime; slotTime <= latestStartTime; slotTime = slotTime.AddMinutes(DefaultSlotIntervalMinutes))
            {
                var slotEndTime = slotTime.AddMinutes(equipment.AvailableTime);
                var isPastTimeToday = targetDate == DateOnly.FromDateTime(currentTaiwanTime)
                    && slotTime <= currentTaiwanTime;

                response.Slots.Add(new FutureReservationSlotItem
                {
                    SlotStartTime = slotTime.ToString("HH:mm"),
                    SlotEndTime = slotEndTime.ToString("HH:mm"),
                    DisplayLabel = $"{slotTime:HH:mm} - {slotEndTime:HH:mm}",
                    IsSelectable = !isPastTimeToday,
                    StatusNote = isPastTimeToday ? "此時段已過，無法選擇" : "可建立未來預約"
                });
            }

            return response;
        }

        private static DateTime GetTaiwanTime()
        {
            try
            {
                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(8);
            }
        }
    }
}
