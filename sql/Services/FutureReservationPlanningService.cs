using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // 這個 Service 專門負責未來時段規劃與推算。
    // 它不直接建立預約，而是先回答兩件事：
    // 1. 這個時段還能不能預約
    // 2. 如果現在預約，到時候大概率是直接使用還是先排隊
    public class FutureReservationPlanningService
    {
        public const int DefaultSlotIntervalMinutes = 15;
        public const int DefaultAdvanceReservationDays = 7;

        private readonly EquipmentService _equipmentService;
        private readonly ReservationRepository _reservationRepository;
        private readonly QueueRepository _queueRepository;

        public FutureReservationPlanningService(
            EquipmentService equipmentService,
            ReservationRepository reservationRepository,
            QueueRepository queueRepository)
        {
            _equipmentService = equipmentService;
            _reservationRepository = reservationRepository;
            _queueRepository = queueRepository;
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

            var currentTaiwanTime = RepositorySqlHelper.GetTaiwanTime();
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
                PlanningNote = "系統會先保留未來預約的時段名額，並依目前使用中與排隊中的情況推算，到時若大概率仍需排隊，會先提醒您再決定是否建立預約。"
            };

            var openDateTime = targetDateTime.Add(equipment.OpenTime);
            var closeDateTime = targetDateTime.Add(equipment.CloseTime);
            var latestStartTime = closeDateTime.AddMinutes(-equipment.AvailableTime);

            for (var slotTime = openDateTime; slotTime <= latestStartTime; slotTime = slotTime.AddMinutes(DefaultSlotIntervalMinutes))
            {
                var slotEndTime = slotTime.AddMinutes(equipment.AvailableTime);
                var isPastTimeToday = targetDate == DateOnly.FromDateTime(currentTaiwanTime)
                    && slotTime <= currentTaiwanTime;

                var forecast = BuildForecast(equipment, slotTime, slotEndTime);
                var isSelectable = !isPastTimeToday && !forecast.HasReservedCapacityConflict;

                response.Slots.Add(new FutureReservationSlotItem
                {
                    SlotStartTime = slotTime.ToString("HH:mm"),
                    SlotEndTime = slotEndTime.ToString("HH:mm"),
                    DisplayLabel = $"{slotTime:HH:mm} - {slotEndTime:HH:mm}",
                    IsSelectable = isSelectable,
                    RequiresQueueConfirmation = isSelectable && forecast.QueueExpected,
                    StatusNote = isPastTimeToday ? "該時段已經過去，不能再建立未來預約" : forecast.Message,
                    ReservedCapacityCount = forecast.ReservedCapacityCount,
                    ForecastWaitingCount = forecast.ForecastWaitingCount
                });
            }

            return response;
        }

        // Repository 建立未來預約前，也會再叫這個推算器確認一次。
        public FutureReservationForecast BuildForecast(byte equipmentId, DateTime reservedStartTime, DateTime reservedEndTime)
        {
            var equipment = _equipmentService.GetEquipment(equipmentId);
            if (equipment == null)
            {
                return new FutureReservationForecast
                {
                    HasReservedCapacityConflict = true,
                    Message = "設備不存在"
                };
            }

            return BuildForecast(equipment, reservedStartTime, reservedEndTime);
        }

        private FutureReservationForecast BuildForecast(Equipment equipment, DateTime reservedStartTime, DateTime reservedEndTime)
        {
            var currentTaiwanTime = RepositorySqlHelper.GetTaiwanTime();
            var minutesUntilStart = Math.Max(0, (int)(reservedStartTime - currentTaiwanTime).TotalMinutes);

            var currentUsers = _queueRepository.GetCurrentUsers(equipment.Id);
            var currentWaitingCount = _queueRepository.GetWaitingQueue(equipment.Id).Count;
            var reservedCapacityCount = _reservationRepository.GetReservedCapacityCount(
                equipment.Id,
                reservedStartTime,
                reservedEndTime);

            if (reservedCapacityCount >= equipment.MaxUsers)
            {
                return new FutureReservationForecast
                {
                    HasReservedCapacityConflict = true,
                    ReservedCapacityCount = reservedCapacityCount,
                    ForecastWaitingCount = currentWaitingCount,
                    Message = "這個時段的保留名額已滿，請改選其他時間"
                };
            }

            // 未來預約本身會先保留名額，所以推算可用容量時要先扣掉。
            var effectiveCapacityAtTarget = Math.Max(0, equipment.MaxUsers - reservedCapacityCount);
            if (effectiveCapacityAtTarget <= 0)
            {
                return new FutureReservationForecast
                {
                    HasReservedCapacityConflict = true,
                    ReservedCapacityCount = reservedCapacityCount,
                    ForecastWaitingCount = currentWaitingCount,
                    Message = "這個時段的容量已被未來預約保留完畢，請改選其他時間"
                };
            }

            // 這裡採用保守推算：
            // 1. 先看現在還有幾個立即可補上的空位
            // 2. 再估算在預約開始前，完整輪轉幾個使用週期
            // 3. 用這些可消化的人數，去扣目前排隊中的人
            var availableStartsNow = Math.Max(0, equipment.MaxUsers - currentUsers);
            var fullCyclesBeforeSlot = equipment.AvailableTime <= 0
                ? 0
                : minutesUntilStart / equipment.AvailableTime;
            var theoreticalStartsBeforeSlot = availableStartsNow + (fullCyclesBeforeSlot * equipment.MaxUsers);
            var forecastWaitingCount = Math.Max(0, currentWaitingCount - theoreticalStartsBeforeSlot);

            if (forecastWaitingCount >= effectiveCapacityAtTarget)
            {
                return new FutureReservationForecast
                {
                    HasReservedCapacityConflict = false,
                    QueueExpected = true,
                    ReservedCapacityCount = reservedCapacityCount,
                    ForecastWaitingCount = forecastWaitingCount,
                    Message = $"依目前隊列推算，到這個時段時前面可能仍有 {forecastWaitingCount} 人在等待。若仍要預約，將視為預約排隊並排入尾端。"
                };
            }

            return new FutureReservationForecast
            {
                HasReservedCapacityConflict = false,
                QueueExpected = false,
                ReservedCapacityCount = reservedCapacityCount,
                ForecastWaitingCount = forecastWaitingCount,
                Message = $"此時段可預約，系統目前預留名額 {reservedCapacityCount}/{equipment.MaxUsers}。"
            };
        }
    }
}
