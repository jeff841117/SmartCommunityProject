using sql.Models;

namespace sql.Services
{
    // 這個 Service 專門負責第二階段的「未來時段預約規劃」。
    // 目前先把可選日期、15 分鐘時段切片、以及今日過去時段過濾整理好，
    // 後續真正建立 Scheduled 預約時，就能直接接在這一層之後。
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

            var targetDate = reservationDate ?? DateOnly.FromDateTime(DateTime.Today);
            var currentTaiwanTime = DateTime.Now;
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
                PlanningNote = "目前先提供時段規劃與前端選擇入口；真正的未來預約建立、衝突檢查與到點轉排隊規則，會在第二階段後續流程落地。"
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
                    StatusNote = isPastTimeToday ? "今日已過時段" : "可規劃預約"
                });
            }

            return response;
        }
    }
}
