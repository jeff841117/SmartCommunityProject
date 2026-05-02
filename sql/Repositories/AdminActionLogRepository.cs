using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // 這個 Repository 專門處理管理者操作紀錄。
    // 目前先涵蓋「寫入」與「查詢列表」兩種責任，
    // 後面若要加詳細頁或條件搜尋，也可以繼續往這裡擴充。
    public class AdminActionLogRepository
    {
        private readonly DBmanager _dbManager;

        public AdminActionLogRepository(DBmanager dbManager)
        {
            _dbManager = dbManager;
        }

        public void CreateLog(AdminActionLogEntry entry)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            using var cmd = new SqlCommand(@"
                INSERT INTO AdminActionLogs
                (
                    AdminUserId,
                    ActionType,
                    TargetType,
                    TargetId,
                    Reason,
                    CreatedAt
                )
                VALUES
                (
                    @AdminUserId,
                    @ActionType,
                    @TargetType,
                    @TargetId,
                    @Reason,
                    @CreatedAt
                )", connection);

            cmd.Parameters.AddWithValue("@AdminUserId", entry.AdminUserId);
            cmd.Parameters.AddWithValue("@ActionType", entry.ActionType);
            cmd.Parameters.AddWithValue("@TargetType", entry.TargetType);
            cmd.Parameters.AddWithValue("@TargetId", entry.TargetId);
            cmd.Parameters.AddWithValue("@Reason", string.IsNullOrWhiteSpace(entry.Reason) ? DBNull.Value : entry.Reason);
            cmd.Parameters.AddWithValue("@CreatedAt", RepositorySqlHelper.GetTaiwanTime());
            cmd.ExecuteNonQuery();
        }

        public List<AdminActionLogListItem> GetRecentLogs(int take = 100)
        {
            var logs = new List<AdminActionLogListItem>();

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT TOP (@Take)
                       l.Id,
                       l.AdminUserId,
                       ISNULL(m.userName, CONCAT('管理者#', l.AdminUserId)) AS AdminUserName,
                       l.ActionType,
                       l.TargetType,
                       l.TargetId,
                       l.Reason,
                       l.CreatedAt
                FROM AdminActionLogs l
                LEFT JOIN member m ON l.AdminUserId = m.id
                ORDER BY l.CreatedAt DESC, l.Id DESC", connection);

            cmd.Parameters.AddWithValue("@Take", take);
            connection.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var actionType = reader.GetInt32(reader.GetOrdinal("ActionType"));
                var targetType = reader.GetInt32(reader.GetOrdinal("TargetType"));

                logs.Add(new AdminActionLogListItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    AdminUserId = reader.GetInt32(reader.GetOrdinal("AdminUserId")),
                    AdminUserName = reader.GetString(reader.GetOrdinal("AdminUserName")),
                    ActionType = actionType,
                    ActionTypeText = AdminActionLogDisplayHelper.GetActionTypeText(actionType),
                    TargetType = targetType,
                    TargetTypeText = AdminActionLogDisplayHelper.GetTargetTypeText(targetType),
                    TargetId = reader.GetInt32(reader.GetOrdinal("TargetId")),
                    Reason = reader.IsDBNull(reader.GetOrdinal("Reason"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Reason")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            return logs;
        }
    }
}
