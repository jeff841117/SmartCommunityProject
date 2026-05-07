using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // AccountRepository 專門負責會員、忘記密碼與帳號管理相關的資料存取。
    // 這裡只處理 SQL 與資料表欄位細節，業務規則交給 AccountService。
    public class AccountRepository
    {
        private const int PasswordColumnMinLength = 255;

        private readonly DBmanager _dbManager;

        public AccountRepository(DBmanager dbManager)
        {
            _dbManager = dbManager;
        }

        public List<account> GetAllAccounts()
        {
            var accounts = new List<account>();

            using var connection = _dbManager.CreateConnection();
            EnsurePasswordColumnCapacity(connection);
            using var command = new SqlCommand(
                "SELECT id, userName, password, age, email, phone, role FROM member ORDER BY id",
                connection);

            EnsureConnectionOpen(connection);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                accounts.Add(MapAccount(reader));
            }

            return accounts;
        }

        public AccountManagementQueryResult GetAccounts(AccountManagementFilter filter)
        {
            var normalizedFilter = NormalizeFilter(filter);
            var items = new List<account>();

            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            EnsurePasswordColumnCapacity(connection);

            var whereParts = BuildWhereParts(normalizedFilter);
            var whereClause = whereParts.Count == 0
                ? string.Empty
                : " WHERE " + string.Join(" AND ", whereParts);

            using var countCommand = new SqlCommand(
                $"SELECT COUNT(*) FROM member{whereClause}",
                connection);
            FillFilterParameters(countCommand, normalizedFilter);
            var totalCount = Convert.ToInt32(countCommand.ExecuteScalar());

            using var command = new SqlCommand($@"
                SELECT id, userName, password, age, email, phone, role
                FROM member
                {whereClause}
                ORDER BY id
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY",
                connection);

            FillFilterParameters(command, normalizedFilter);
            command.Parameters.AddWithValue("@offset", (normalizedFilter.Page - 1) * normalizedFilter.PageSize);
            command.Parameters.AddWithValue("@pageSize", normalizedFilter.PageSize);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(MapAccount(reader));
            }

            return new AccountManagementQueryResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = normalizedFilter.Page,
                PageSize = normalizedFilter.PageSize
            };
        }

        public account? GetAccountByUserName(string username)
        {
            using var connection = _dbManager.CreateConnection();
            EnsurePasswordColumnCapacity(connection);
            using var command = new SqlCommand(@"
                SELECT TOP 1 id, userName, password, age, email, phone, role
                FROM member
                WHERE userName = @userName",
                connection);

            command.Parameters.AddWithValue("@userName", username);

            EnsureConnectionOpen(connection);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapAccount(reader) : null;
        }

        public account? GetAccountByEmail(string email)
        {
            using var connection = _dbManager.CreateConnection();
            EnsurePasswordColumnCapacity(connection);
            using var command = new SqlCommand(@"
                SELECT TOP 1 id, userName, password, age, email, phone, role
                FROM member
                WHERE email = @email",
                connection);

            command.Parameters.AddWithValue("@email", email);

            EnsureConnectionOpen(connection);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapAccount(reader) : null;
        }

        public PasswordResetCodeRecord? GetLatestPasswordResetCode(string email, string code)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                SELECT TOP 1 Id, UserId, Email, Code, ExpiredAt, UsedAt, Status
                FROM PasswordResetCodes
                WHERE Email = @email AND Code = @code
                ORDER BY Id DESC",
                connection);

            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@code", code);

            EnsureConnectionOpen(connection);
            using var reader = command.ExecuteReader();
            return reader.Read() ? MapPasswordResetCode(reader) : null;
        }

        public void CancelActivePasswordResetCodes(int userId, string email)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                UPDATE PasswordResetCodes
                SET Status = 4
                WHERE UserId = @userId
                  AND Email = @email
                  AND Status = 1",
                connection);

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@email", email);

            EnsureConnectionOpen(connection);
            command.ExecuteNonQuery();
        }

        public void CreatePasswordResetCode(PasswordResetCodeRecord resetCode)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                INSERT INTO PasswordResetCodes(UserId, Email, Code, ExpiredAt, UsedAt, Status)
                VALUES(@userId, @email, @code, @expiredAt, @usedAt, @status)",
                connection);

            command.Parameters.AddWithValue("@userId", resetCode.UserId);
            command.Parameters.AddWithValue("@email", resetCode.Email);
            command.Parameters.AddWithValue("@code", resetCode.Code);
            command.Parameters.AddWithValue("@expiredAt", resetCode.ExpiredAt);
            command.Parameters.AddWithValue("@usedAt", resetCode.UsedAt ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", resetCode.Status);

            EnsureConnectionOpen(connection);
            command.ExecuteNonQuery();
        }

        public void MarkPasswordResetCodeAsExpired(int id)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                UPDATE PasswordResetCodes
                SET Status = 3
                WHERE Id = @id",
                connection);

            command.Parameters.AddWithValue("@id", id);

            EnsureConnectionOpen(connection);
            command.ExecuteNonQuery();
        }

        public void MarkPasswordResetCodeAsVerified(int id)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                UPDATE PasswordResetCodes
                SET Status = 2,
                    UsedAt = GETDATE()
                WHERE Id = @id",
                connection);

            command.Parameters.AddWithValue("@id", id);

            EnsureConnectionOpen(connection);
            command.ExecuteNonQuery();
        }

        public void UpdatePasswordByUserId(int userId, string newPassword)
        {
            using var connection = _dbManager.CreateConnection();
            EnsurePasswordColumnCapacity(connection);
            using var command = new SqlCommand(@"
                UPDATE member
                SET password = @password
                WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", userId);
            command.Parameters.AddWithValue("@password", newPassword);

            EnsureConnectionOpen(connection);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new Exception("找不到要更新密碼的帳號。");
            }
        }

        public void CreateAccount(account user)
        {
            using var connection = _dbManager.CreateConnection();
            EnsurePasswordColumnCapacity(connection);
            using var command = new SqlCommand(@"
                INSERT INTO member(userName, password, age, email, phone, role)
                VALUES(@userName, @password, @age, @email, @phone, @role)",
                connection);

            FillCreateParameters(command, user);

            EnsureConnectionOpen(connection);
            command.ExecuteNonQuery();
        }

        public void UpdateAccount(account user)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            EnsurePasswordColumnCapacity(connection);

            var updatePassword = !string.IsNullOrWhiteSpace(user.password);
            using var command = new SqlCommand(updatePassword
                ? @"
                    UPDATE member
                    SET password = @password,
                        email = @email,
                        phone = @phone
                    WHERE id = @id"
                : @"
                    UPDATE member
                    SET email = @email,
                        phone = @phone
                    WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", user.id);
            command.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(user.email) ? DBNull.Value : user.email);
            command.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(user.phone) ? DBNull.Value : user.phone);

            if (updatePassword)
            {
                command.Parameters.AddWithValue("@password", user.password);
            }

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new Exception("找不到要更新的帳號資料。");
            }
        }

        // 舊資料庫的 password 欄位可能仍是短字串長度，無法容納雜湊後的密碼。
        // 在真正讀寫會員資料前先補一次欄位容量，避免人工登入與重設密碼時被資料庫截斷。
        private static void EnsurePasswordColumnCapacity(SqlConnection connection)
        {
            var wasClosed = connection.State != System.Data.ConnectionState.Open;
            if (wasClosed)
            {
                connection.Open();
            }

            using var checkCommand = new SqlCommand(
                "SELECT COL_LENGTH('member', 'password')",
                connection);

            var currentLength = checkCommand.ExecuteScalar();
            if (currentLength is int length && length >= PasswordColumnMinLength * 2)
            {
                return;
            }

            using var alterCommand = new SqlCommand(
                $"ALTER TABLE member ALTER COLUMN password NVARCHAR({PasswordColumnMinLength}) NOT NULL;",
                connection);
            alterCommand.ExecuteNonQuery();
        }

        private static void EnsureConnectionOpen(SqlConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
        }

        private static account MapAccount(SqlDataReader reader)
        {
            return new account
            {
                id = reader.GetInt32(reader.GetOrdinal("id")),
                userName = reader.GetString(reader.GetOrdinal("userName")),
                password = reader.GetString(reader.GetOrdinal("password")),
                age = reader.GetDouble(reader.GetOrdinal("age")),
                email = reader.IsDBNull(reader.GetOrdinal("email"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("email")),
                phone = reader.IsDBNull(reader.GetOrdinal("phone"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("phone")),
                role = reader.IsDBNull(reader.GetOrdinal("role"))
                    ? "user"
                    : reader.GetString(reader.GetOrdinal("role"))
            };
        }

        private static PasswordResetCodeRecord MapPasswordResetCode(SqlDataReader reader)
        {
            return new PasswordResetCodeRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Code = reader.GetString(reader.GetOrdinal("Code")),
                ExpiredAt = reader.GetDateTime(reader.GetOrdinal("ExpiredAt")),
                UsedAt = reader.IsDBNull(reader.GetOrdinal("UsedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UsedAt")),
                Status = reader.GetInt32(reader.GetOrdinal("Status"))
            };
        }

        private static AccountManagementFilter NormalizeFilter(AccountManagementFilter filter)
        {
            return new AccountManagementFilter
            {
                SearchField = NormalizeSearchField(filter.SearchField),
                Keyword = string.IsNullOrWhiteSpace(filter.Keyword) ? null : filter.Keyword.Trim(),
                ExactMatch = filter.ExactMatch,
                Role = string.IsNullOrWhiteSpace(filter.Role) ? null : filter.Role.Trim(),
                Page = filter.Page <= 0 ? 1 : filter.Page,
                PageSize = filter.PageSize <= 0 ? 10 : Math.Min(filter.PageSize, 100)
            };
        }

        private static string NormalizeSearchField(string? searchField)
        {
            return searchField switch
            {
                "email" => "email",
                "phone" => "phone",
                _ => "userName"
            };
        }

        private static List<string> BuildWhereParts(AccountManagementFilter filter)
        {
            var whereParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var columnName = filter.SearchField switch
                {
                    "email" => "email",
                    "phone" => "phone",
                    _ => "userName"
                };

                whereParts.Add(filter.ExactMatch
                    ? $"{columnName} = @keyword"
                    : $"{columnName} LIKE @keyword");
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                whereParts.Add("role = @role");
            }

            return whereParts;
        }

        private static void FillFilterParameters(SqlCommand command, AccountManagementFilter filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                command.Parameters.AddWithValue(
                    "@keyword",
                    filter.ExactMatch ? filter.Keyword! : $"%{filter.Keyword}%");
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                command.Parameters.AddWithValue("@role", filter.Role!);
            }
        }

        private static void FillCreateParameters(SqlCommand command, account user)
        {
            command.Parameters.AddWithValue("@userName", user.userName);
            command.Parameters.AddWithValue("@password", user.password);
            command.Parameters.AddWithValue("@age", user.age);
            command.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(user.email) ? DBNull.Value : user.email);
            command.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(user.phone) ? DBNull.Value : user.phone);
            command.Parameters.AddWithValue("@role", string.IsNullOrWhiteSpace(user.role) ? "user" : user.role);
        }
    }
}
