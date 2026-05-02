using System.Text;
using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // 管理者操作紀錄 Repository 的責任很單純：
    // 1. 寫入操作紀錄
    // 2. 依篩選條件查詢操作紀錄
    // 3. 提供匯出用的資料清單
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

        public AdminActionLogQueryResult GetLogs(AdminActionLogFilter filter)
        {
            var normalizedFilter = NormalizeFilter(filter);
            var logs = new List<AdminActionLogListItem>();

            using var connection = _dbManager.CreateConnection();
            connection.Open();

            var whereClause = BuildWhereClause(normalizedFilter);

            using (var countCmd = new SqlCommand($@"
                SELECT COUNT(1)
                FROM AdminActionLogs l
                LEFT JOIN member m ON l.AdminUserId = m.id
                {whereClause}", connection))
            {
                AddFilterParameters(countCmd, normalizedFilter);
                normalizedFilter.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
            }

            using (var dataCmd = new SqlCommand(BuildPagedQuery(whereClause), connection))
            {
                AddFilterParameters(dataCmd, normalizedFilter);

                using var reader = dataCmd.ExecuteReader();
                while (reader.Read())
                {
                    logs.Add(ReadListItem(reader));
                }
            }

            return new AdminActionLogQueryResult
            {
                Items = logs,
                TotalCount = normalizedFilter.TotalCount,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            };
        }

        public List<AdminActionLogListItem> ExportLogs(AdminActionLogFilter filter, int maxRows = 1000)
        {
            var normalizedFilter = NormalizeFilter(filter);
            var logs = new List<AdminActionLogListItem>();

            using var connection = _dbManager.CreateConnection();
            connection.Open();

            var whereClause = BuildWhereClause(normalizedFilter);

            using var cmd = new SqlCommand(BuildExportQuery(whereClause, maxRows), connection);
            AddFilterParameters(cmd, normalizedFilter, includePaging: false);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(ReadListItem(reader));
            }

            return logs;
        }

        private static AdminActionLogFilterInternal NormalizeFilter(AdminActionLogFilter filter)
        {
            return new AdminActionLogFilterInternal
            {
                AdminKeyword = filter.AdminKeyword?.Trim(),
                ActionType = filter.ActionType > 0 ? filter.ActionType : null,
                TargetType = filter.TargetType > 0 ? filter.TargetType : null,
                Keyword = filter.Keyword?.Trim(),
                StartDate = filter.StartDate?.Date,
                EndDate = filter.EndDate?.Date,
                Page = filter.Page <= 0 ? 1 : filter.Page,
                PageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 100)
            };
        }

        private static string BuildWhereClause(AdminActionLogFilterInternal filter)
        {
            var sql = new StringBuilder("WHERE 1 = 1");

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

            return sql.ToString();
        }

        private static string BuildPagedQuery(string whereClause)
        {
            return $@"
                SELECT
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
                {whereClause}
                ORDER BY l.CreatedAt DESC, l.Id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        }

        private static string BuildExportQuery(string whereClause, int maxRows)
        {
            return $@"
                SELECT TOP ({maxRows})
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
                {whereClause}
                ORDER BY l.CreatedAt DESC, l.Id DESC";
        }

        private static void AddFilterParameters(
            SqlCommand cmd,
            AdminActionLogFilterInternal filter,
            bool includePaging = true)
        {
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

            if (includePaging)
            {
                cmd.Parameters.AddWithValue("@Offset", (filter.Page - 1) * filter.PageSize);
                cmd.Parameters.AddWithValue("@PageSize", filter.PageSize);
            }
        }

        private static AdminActionLogListItem ReadListItem(SqlDataReader reader)
        {
            var actionType = reader.GetInt32(reader.GetOrdinal("ActionType"));
            var targetType = reader.GetInt32(reader.GetOrdinal("TargetType"));

            return new AdminActionLogListItem
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
            };
        }

        private sealed class AdminActionLogFilterInternal
        {
            public string? AdminKeyword { get; set; }
            public int? ActionType { get; set; }
            public int? TargetType { get; set; }
            public string? Keyword { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
        }
    }
}
