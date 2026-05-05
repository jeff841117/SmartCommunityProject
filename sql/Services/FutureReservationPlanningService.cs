using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // 這一層專門負責「未來時段規劃」，
    // 讓前台選日期後，可以拿到當天還有效、且符合規則的時間點。
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
                PlanningNote = string.Empty
            };

            var openDateTime = targetDateTime.Add(equipment.OpenTime);
            var closeDateTime = targetDateTime.Add(equipment.CloseTime);
            var latestStartTime = closeDateTime.AddMinutes(-equipment.AvailableTime);

            for (var slotTime = openDateTime; slotTime <= latestStartTime; slotTime = slotTime.AddMinutes(DefaultSlotIntervalMinutes))
            {
                var slotEndTime = slotTime.AddMinutes(equipment.AvailableTime);
                var isPastTimeToday = targetDate == DateOnly.FromDateTime(currentTaiwanTime)
                    && slotTime <= currentTaiwanTime;

                // 使用者要求過去時段直接不要顯示，
                // 所以這裡直接略過，而不是先顯示再標成不可選。
                if (isPastTimeToday)
                {
                    continue;
                }

                var forecast = BuildForecast(equipment, slotTime, slotEndTime);
                var isSelectable = !forecast.HasReservedCapacityConflict;

                response.Slots.Add(new FutureReservationSlotItem
                {
                    SlotStartTime = slotTime.ToString("HH:mm"),
                    SlotEndTime = slotEndTime.ToString("HH:mm"),
                    // 前台下拉只需要顯示開始時間，
                    // 可使用多久由設備本身的「使用時間」說明承擔。
                    DisplayLabel = slotTime.ToString("HH:mm"),
                    IsSelectable = isSelectable,
                    RequiresQueueConfirmation = isSelectable && forecast.QueueExpected,
                    StatusNote = forecast.Message,
                    ReservedCapacityCount = forecast.ReservedCapacityCount,
                    ForecastWaitingCount = forecast.ForecastWaitingCount
                });
            }

            return response;
        }

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
                    Message = $"依目前推算，到這個時段時前方仍可能有 {forecastWaitingCount} 人等待，建立後會先視為預約排隊。"
                };
            }

            return new FutureReservationForecast
            {
                HasReservedCapacityConflict = false,
                QueueExpected = false,
                ReservedCapacityCount = reservedCapacityCount,
                ForecastWaitingCount = forecastWaitingCount,
                Message = $"此時段目前可規劃，已保留名額 {reservedCapacityCount}/{equipment.MaxUsers}。"
            };
        }
    }
}
