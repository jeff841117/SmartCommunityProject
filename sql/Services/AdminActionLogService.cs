using System.Text;
using sql.Models;
using sql.Repositories;

namespace sql.Services
{
    // 這個 Service 專門負責管理者操作紀錄。
    // 1. 在成功操作後寫入日誌
    // 2. 查詢與篩選紀錄
    // 3. 匯出 CSV
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
                Reason = "管理者強制結束使用中的預約"
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
                Reason = "管理者取消未開始的未來預約"
            });
        }

        public void LogForceRescheduleScheduledReservation(
            int adminUserId,
            int reservationId,
            string reservationDate,
            string selectedSlotStartTime,
            bool queueExpected)
        {
            var queueHint = queueExpected ? "，並標記為預計需排隊" : string.Empty;

            WriteLog(new AdminActionLogEntry
            {
                AdminUserId = adminUserId,
                ActionType = (int)AdminActionType.ForceRescheduleScheduledReservation,
                TargetType = (int)AdminActionTargetType.Reservation,
                TargetId = reservationId,
                Reason = $"管理者將預約調整為 {reservationDate} {selectedSlotStartTime}{queueHint}"
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

            csv.AppendLine("流水號,建立時間,管理者編號,管理者名稱,操作類型,目標類型,目標編號,操作原因");

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

            // 加上 UTF-8 BOM，避免 Excel 開啟中文時亂碼。
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
                // 寫日誌失敗時不要反過來中斷主要流程，但仍保留錯誤訊息方便追查。
                Console.WriteLine($"寫入管理者操作紀錄失敗：{ex.Message}");
            }
        }
    }
}
