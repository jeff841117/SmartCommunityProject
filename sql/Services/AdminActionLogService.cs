using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // 這層負責把管理者操作意圖整理成可讀的日誌內容。
    // Controller、ReservationService、QueueService 不需要自己拼 action type 與 reason，
    // 只要呼叫對應方法即可。
    public class AdminActionLogService
    {
        private readonly AdminActionLogRepository _adminActionLogRepository;

        public AdminActionLogService(AdminActionLogRepository adminActionLogRepository)
        {
            _adminActionLogRepository = adminActionLogRepository;
        }

        public void LogForceEndUsage(int adminUserId, int reservationId)
        {
            WriteLog(new AdminActionLogEntry
            {
                AdminUserId = adminUserId,
                ActionType = (int)AdminActionType.ForceEndUsage,
                TargetType = (int)AdminActionTargetType.Reservation,
                TargetId = reservationId,
                Reason = "管理者於後台強制結束使用"
            });
        }

        public void LogForceCancelScheduledReservation(int adminUserId, int reservationId)
        {
            WriteLog(new AdminActionLogEntry
            {
                AdminUserId = adminUserId,
                ActionType = (int)AdminActionType.ForceCancelScheduledReservation,
                TargetType = (int)AdminActionTargetType.Reservation,
                TargetId = reservationId,
                Reason = "管理者於後台取消未來預約"
            });
        }

        public void LogForceRescheduleScheduledReservation(
            int adminUserId,
            int reservationId,
            string reservationDate,
            string selectedSlotStartTime,
            bool queueExpected)
        {
            var queueHint = queueExpected ? "，系統推算到時仍可能需要排隊" : string.Empty;

            WriteLog(new AdminActionLogEntry
            {
                AdminUserId = adminUserId,
                ActionType = (int)AdminActionType.ForceRescheduleScheduledReservation,
                TargetType = (int)AdminActionTargetType.Reservation,
                TargetId = reservationId,
                Reason = $"管理者將預約改到 {reservationDate} {selectedSlotStartTime}{queueHint}"
            });
        }

        public void LogForceCancelQueue(int adminUserId, int queueId)
        {
            WriteLog(new AdminActionLogEntry
            {
                AdminUserId = adminUserId,
                ActionType = (int)AdminActionType.ForceCancelQueue,
                TargetType = (int)AdminActionTargetType.WaitingQueue,
                TargetId = queueId,
                Reason = "管理者於後台移除排隊紀錄"
            });
        }

        public List<AdminActionLogListItem> GetRecentLogs(int take = 100)
        {
            return _adminActionLogRepository.GetRecentLogs(take);
        }

        private void WriteLog(AdminActionLogEntry entry)
        {
            try
            {
                _adminActionLogRepository.CreateLog(entry);
            }
            catch (Exception ex)
            {
                // 操作紀錄不應阻斷主要功能，所以這裡只記錄主控台訊息。
                Console.WriteLine($"寫入管理者操作紀錄時發生錯誤: {ex.Message}");
            }
        }
    }
}
