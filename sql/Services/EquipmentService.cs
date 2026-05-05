using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // EquipmentService 負責設備模組的業務流程。
    // 目前設備流程還不算複雜，但先加上這一層，後面擴充後台設備規則會比較好接。
    public class EquipmentService
    {
        private readonly EquipmentRepository _equipmentRepository;

        public EquipmentService(EquipmentRepository equipmentRepository)
        {
            _equipmentRepository = equipmentRepository;
        }

        public List<Equipment> GetAllEquipments()
        {
            return _equipmentRepository.GetAll();
        }

        public Equipment? GetEquipment(byte equipmentId)
        {
            return _equipmentRepository.GetById(equipmentId);
        }

        public void CreateEquipment(Equipment equipment)
        {
            if (string.IsNullOrWhiteSpace(equipment.EquipmentCategory))
            {
                equipment.EquipmentCategory = "場館";
            }

            _equipmentRepository.Create(equipment);
        }

        public void UpdateEquipment(Equipment equipment)
        {
            if (string.IsNullOrWhiteSpace(equipment.EquipmentCategory))
            {
                equipment.EquipmentCategory = "場館";
            }

            _equipmentRepository.Update(equipment);
        }

        public void DeleteEquipment(byte equipmentId)
        {
            _equipmentRepository.Delete(equipmentId);
        }

        // 這個方法把設備可用性檢查集中起來，
        // 讓 Controller 不用自己組 if / else 判斷。
        public EquipmentAvailabilityResponse CheckAvailability(byte equipmentId)
        {
            var equipment = _equipmentRepository.GetById(equipmentId);
            if (equipment == null)
            {
                return new EquipmentAvailabilityResponse
                {
                    IsAvailable = false,
                    CanReserve = false,
                    IsFull = false,
                    Message = "設備不存在",
                    ServerTaiwanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }

            var currentTime = DateTime.Now.TimeOfDay;
            var isInOperatingHours = currentTime >= equipment.OpenTime && currentTime <= equipment.CloseTime;
            var currentUsers = _equipmentRepository.GetCurrentUsers(equipmentId);
            var isWithinCapacity = currentUsers < equipment.MaxUsers;

            return new EquipmentAvailabilityResponse
            {
                IsAvailable = isInOperatingHours && isWithinCapacity,
                CanReserve = isInOperatingHours,
                IsFull = !isWithinCapacity,
                Message = isInOperatingHours ? (isWithinCapacity ? "可預約" : "設備已滿") : "非開放時間",
                CurrentUsers = currentUsers,
                MaxUsers = equipment.MaxUsers,
                AverageUsageTime = equipment.AvailableTime,
                ServerTaiwanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                OpenTime = equipment.OpenTime.ToString(@"hh\:mm"),
                CloseTime = equipment.CloseTime.ToString(@"hh\:mm")
            };
        }
    }
}
