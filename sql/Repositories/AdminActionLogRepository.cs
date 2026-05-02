using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // 這個 Repository 專門負責把管理者操作寫進 AdminActionLogs。
    // 先把寫入集中起來，之後若要加查詢頁或篩選條件，就不需要再到各個模組分別找 SQL。
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
    }
}
