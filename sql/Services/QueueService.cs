using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // QueueService 專門協調排隊相關流程。
    // ReservationService 偏預約主流程，QueueService 偏排隊狀態與排隊操作。
    public class QueueService
    {
        private readonly QueueRepository _queueRepository;

        public QueueService(QueueRepository queueRepository)
        {
            _queueRepository = queueRepository;
        }

        public void ProcessAllQueues()
        {
            _queueRepository.ProcessAllQueues();
        }

        public void ProcessEquipmentQueue(byte equipmentId)
        {
            _queueRepository.ProcessEquipmentQueue(equipmentId);
        }

        // 給管理 / 偵錯看的完整排隊資訊。
        public QueueDebugInfoResponse GetQueueDebugInfo(byte equipmentId)
        {
            var equipment = _queueRepository.GetEquipmentById(equipmentId);
            var currentUsers = _queueRepository.GetCurrentUsers(equipmentId);
            var waitingList = _queueRepository.GetWaitingQueue(equipmentId);

            return new QueueDebugInfoResponse
            {
                Equipment = equipment?.equipmentName ?? string.Empty,
                CurrentUsers = currentUsers,
                MaxUsers = equipment?.MaxUsers ?? 0,
                QueueCount = waitingList.Count,
                HasVacancy = currentUsers < (equipment?.MaxUsers ?? 0),
                WaitingUsers = waitingList.Select(q => new QueueWaitingUser
                {
                    UserId = q.UserId,
                    Position = q.Position
                }).ToList()
            };
        }

        // 給一般前端畫面使用的排隊資訊。
        public QueueInfoResponse GetQueueInfo(byte equipmentId)
        {
            var queue = _queueRepository.GetWaitingQueue(equipmentId);
            var currentUsers = _queueRepository.GetCurrentUsers(equipmentId);
            var equipment = _queueRepository.GetEquipmentById(equipmentId);

            return new QueueInfoResponse
            {
                WaitingCount = queue.Count,
                CurrentUsers = currentUsers,
                MaxUsers = equipment?.MaxUsers ?? 0,
                QueueList = queue.Select(q => new QueueListItem
                {
                    UserId = q.UserId,
                    Position = q.Position,
                    QueueTime = q.QueueTime
                }).ToList()
            };
        }

        // 取消排隊前先驗證是否登入。
        public bool CancelQueue(int queueId, CurrentUser currentUser)
        {
            if (!currentUser.IsAuthenticated)
            {
                return false;
            }

            return _queueRepository.CancelQueue(queueId, currentUser.ReservationUserKey);
        }

        public bool ForceCancelQueue(int queueId, CurrentUser currentUser)
        {
            if (!currentUser.IsManager)
            {
                return false;
            }

            return _queueRepository.ForceCancelQueue(queueId, currentUser.UserId);
        }
    }
}
