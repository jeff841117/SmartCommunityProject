using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // 這個 Service 主要做兩件事：
    // 1. 把管理者操作轉成一致的日誌格式
    // 2. 提供後台查詢頁一個乾淨的查詢入口
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
                Reason = "管理者強制結束使用中預約"
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
                Reason = "管理者取消未來預約"
            });
        }

        public void LogForceRescheduleScheduledReservation(
            int adminUserId,
            int reservationId,
            string reservationDate,
            string selectedSlotStartTime,
            bool queueExpected)
        {
            var queueHint = queueExpected ? "，調整後預估仍需排隊" : string.Empty;

            WriteLog(new AdminActionLogEntry
            {
                AdminUserId = adminUserId,
                ActionType = (int)AdminActionType.ForceRescheduleScheduledReservation,
                TargetType = (int)AdminActionTargetType.Reservation,
                TargetId = reservationId,
                Reason = $"管理者調整未來預約至 {reservationDate} {selectedSlotStartTime}{queueHint}"
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
                Reason = "管理者移除排隊紀錄"
            });
        }

        public List<AdminActionLogListItem> GetRecentLogs(AdminActionLogFilter? filter = null)
        {
            return _adminActionLogRepository.GetRecentLogs(filter ?? new AdminActionLogFilter());
        }

        private void WriteLog(AdminActionLogEntry entry)
        {
            try
            {
                _adminActionLogRepository.CreateLog(entry);
            }
            catch (Exception ex)
            {
                // 寫日誌失敗不應阻擋主要功能，所以這裡只記錄錯誤。
                Console.WriteLine($"寫入管理者操作日誌失敗：{ex.Message}");
            }
        }
    }
}
