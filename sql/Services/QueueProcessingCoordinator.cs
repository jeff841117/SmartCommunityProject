using sql.Repositories;

namespace sql.Services
{
    // QueueProcessingCoordinator 是給其他模組呼叫的「排隊推進入口」。
    // 它的目的不是取代 QueueService，
    // 而是避免 ReservationRepository 直接回頭依賴舊 bridge。
    public class QueueProcessingCoordinator
    {
        private readonly QueueRepository _queueRepository;

        public QueueProcessingCoordinator(QueueRepository queueRepository)
        {
            _queueRepository = queueRepository;
        }

        public void ProcessEquipmentQueue(byte equipmentId)
        {
            _queueRepository.ProcessEquipmentQueue(equipmentId);
        }

        public void ProcessAllQueues()
        {
            _queueRepository.ProcessAllQueues();
        }
    }
}
