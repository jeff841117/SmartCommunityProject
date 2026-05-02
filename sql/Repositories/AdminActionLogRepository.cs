using System.Text;
using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // 管理者操作紀錄 Repository 的責任很單純：
    // 1. 寫入操作紀錄
    // 2. 依篩選條件查詢操作紀錄
    // 這樣後面如果要改成分頁或匯出，只需要從這層擴充。
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

        public List<AdminActionLogListItem> GetRecentLogs(AdminActionLogFilter filter)
        {
            var logs = new List<AdminActionLogListItem>();
            var normalizedFilter = NormalizeFilter(filter);

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(BuildQuery(normalizedFilter), connection);
            AddFilterParameters(cmd, normalizedFilter);

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

        private static AdminActionLogFilter NormalizeFilter(AdminActionLogFilter filter)
        {
            return new AdminActionLogFilter
            {
                AdminKeyword = filter.AdminKeyword?.Trim(),
                ActionType = filter.ActionType > 0 ? filter.ActionType : null,
                TargetType = filter.TargetType > 0 ? filter.TargetType : null,
                Keyword = filter.Keyword?.Trim(),
                StartDate = filter.StartDate?.Date,
                EndDate = filter.EndDate?.Date,
                Take = filter.Take <= 0 ? 100 : Math.Min(filter.Take, 500)
            };
        }

        private static string BuildQuery(AdminActionLogFilter filter)
        {
            var sql = new StringBuilder(@"
                SELECT TOP (@Take)
                       l.Id,
                       l.AdminUserId,
                       ISNULL(m.userName, CONCAT('管理員#', l.AdminUserId)) AS AdminUserName,
                       l.ActionType,
                       l.TargetType,
                       l.TargetId,
                       l.Reason,
                       l.CreatedAt
                FROM AdminActionLogs l
                LEFT JOIN member m ON l.AdminUserId = m.id
                WHERE 1 = 1");

            if (!string.IsNullOrWhiteSpace(filter.AdminKeyword))
            {
                sql.Append(@"
                  AND
                  (
                      m.userName LIKE @AdminKeyword
                      OR CONVERT(NVARCHAR(20), l.AdminUserId) LIKE @AdminKeyword
                  )");
            }

            if (filter.ActionType.HasValue)
            {
                sql.Append(@"
                  AND l.ActionType = @ActionType");
            }

            if (filter.TargetType.HasValue)
            {
                sql.Append(@"
                  AND l.TargetType = @TargetType");
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                sql.Append(@"
                  AND
                  (
                      ISNULL(l.Reason, '') LIKE @Keyword
                      OR CONVERT(NVARCHAR(20), l.TargetId) LIKE @Keyword
                  )");
            }

            if (filter.StartDate.HasValue)
            {
                sql.Append(@"
                  AND l.CreatedAt >= @StartDate");
            }

            if (filter.EndDate.HasValue)
            {
                sql.Append(@"
                  AND l.CreatedAt < @EndExclusive");
            }

            sql.Append(@"
                ORDER BY l.CreatedAt DESC, l.Id DESC");

            return sql.ToString();
        }

        private static void AddFilterParameters(SqlCommand cmd, AdminActionLogFilter filter)
        {
            cmd.Parameters.AddWithValue("@Take", filter.Take);

            if (!string.IsNullOrWhiteSpace(filter.AdminKeyword))
            {
                cmd.Parameters.AddWithValue("@AdminKeyword", $"%{filter.AdminKeyword}%");
            }

            if (filter.ActionType.HasValue)
            {
                cmd.Parameters.AddWithValue("@ActionType", filter.ActionType.Value);
            }

            if (filter.TargetType.HasValue)
            {
                cmd.Parameters.AddWithValue("@TargetType", filter.TargetType.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                cmd.Parameters.AddWithValue("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.StartDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@StartDate", filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@EndExclusive", filter.EndDate.Value.AddDays(1));
            }
        }
    }
}
