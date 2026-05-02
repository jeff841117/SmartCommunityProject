using System.Text;
using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // 這個 Service 主要做三件事：
    // 1. 把管理者操作轉成一致的日誌格式
    // 2. 提供後台查詢頁分頁查詢入口
    // 3. 提供匯出 CSV 前需要的資料
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

        public AdminActionLogQueryResult GetLogs(AdminActionLogFilter? filter = null)
        {
            return _adminActionLogRepository.GetLogs(filter ?? new AdminActionLogFilter());
        }

        public byte[] ExportLogsAsCsv(AdminActionLogFilter? filter = null)
        {
            var logs = _adminActionLogRepository.ExportLogs(filter ?? new AdminActionLogFilter());
            var csv = new StringBuilder();

            csv.AppendLine("紀錄編號,時間,管理者編號,管理者名稱,操作類型,目標類型,目標編號,原因說明");

            foreach (var log in logs)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(log.Id.ToString()),
                    EscapeCsv(log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsv(log.AdminUserId.ToString()),
                    EscapeCsv(log.AdminUserName),
                    EscapeCsv(log.ActionTypeText),
                    EscapeCsv(log.TargetTypeText),
                    EscapeCsv(log.TargetId.ToString()),
                    EscapeCsv(log.Reason ?? string.Empty)));
            }

            // 用 UTF-8 BOM 讓 Excel 開啟時能正確顯示中文。
            var bom = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(csv.ToString());
            return [.. bom, .. content];
        }

        private static string EscapeCsv(string value)
        {
            var normalized = value.Replace("\"", "\"\"");
            return $"\"{normalized}\"";
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
