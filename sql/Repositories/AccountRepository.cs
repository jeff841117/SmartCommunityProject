using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // AccountRepository 專門處理 member 資料表的讀寫。
    // 這裡會順手做舊資料相容，例如補欄位、補預設值、擴充密碼欄位長度。
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
            return GetAccounts(new AccountManagementFilter
            {
                Page = 1,
                PageSize = 500
            }).Items;
        }

        public AccountManagementQueryResult GetAccounts(AccountManagementFilter filter)
        {
            var normalizedFilter = NormalizeFilter(filter);
            var items = new List<account>();

            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            EnsureMemberSchema(connection);

            var whereParts = BuildWhereParts(normalizedFilter);
            var whereClause = whereParts.Count == 0
                ? string.Empty
                : " WHERE " + string.Join(" AND ", whereParts);

            using var countCommand = new SqlCommand($"SELECT COUNT(*) FROM member{whereClause}", connection);
            FillFilterParameters(countCommand, normalizedFilter);
            var totalCount = Convert.ToInt32(countCommand.ExecuteScalar());

            using var command = new SqlCommand($@"
                SELECT id, userName, password, age, email, phone, role, isActive
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
            EnsureConnectionOpen(connection);
            EnsureMemberSchema(connection);

            using var command = new SqlCommand(@"
                SELECT TOP 1 id, userName, password, age, email, phone, role, isActive
                FROM member
                WHERE userName = @userName", connection);
            command.Parameters.AddWithValue("@userName", username);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapAccount(reader) : null;
        }

        public account? GetAccountByEmail(string email)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            EnsureMemberSchema(connection);

            using var command = new SqlCommand(@"
                SELECT TOP 1 id, userName, password, age, email, phone, role, isActive
                FROM member
                WHERE email = @email", connection);
            command.Parameters.AddWithValue("@email", email);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapAccount(reader) : null;
        }

        public PasswordResetCodeRecord? GetLatestPasswordResetCode(string email, string code)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);

            using var command = new SqlCommand(@"
                SELECT TOP 1 Id, UserId, Email, Code, ExpiredAt, UsedAt, Status
                FROM PasswordResetCodes
                WHERE Email = @email AND Code = @code
                ORDER BY Id DESC", connection);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@code", code);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapPasswordResetCode(reader) : null;
        }

        public void CancelActivePasswordResetCodes(int userId, string email)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);

            using var command = new SqlCommand(@"
                UPDATE PasswordResetCodes
                SET Status = 4
                WHERE UserId = @userId
                  AND Email = @email
                  AND Status = 1", connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@email", email);
            command.ExecuteNonQuery();
        }

        public void CreatePasswordResetCode(PasswordResetCodeRecord resetCode)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);

            using var command = new SqlCommand(@"
                INSERT INTO PasswordResetCodes(UserId, Email, Code, ExpiredAt, UsedAt, Status)
                VALUES(@userId, @email, @code, @expiredAt, @usedAt, @status)", connection);
            command.Parameters.AddWithValue("@userId", resetCode.UserId);
            command.Parameters.AddWithValue("@email", resetCode.Email);
            command.Parameters.AddWithValue("@code", resetCode.Code);
            command.Parameters.AddWithValue("@expiredAt", resetCode.ExpiredAt);
            command.Parameters.AddWithValue("@usedAt", resetCode.UsedAt ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", resetCode.Status);
            command.ExecuteNonQuery();
        }

        public void MarkPasswordResetCodeAsExpired(int id)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            using var command = new SqlCommand("UPDATE PasswordResetCodes SET Status = 3 WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public void MarkPasswordResetCodeAsVerified(int id)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            using var command = new SqlCommand(@"
                UPDATE PasswordResetCodes
                SET Status = 2,
                    UsedAt = GETDATE()
                WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public void UpdatePasswordByUserId(int userId, string newPassword)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            EnsureMemberSchema(connection);

            using var command = new SqlCommand(@"
                UPDATE member
                SET password = @password
                WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", userId);
            command.Parameters.AddWithValue("@password", newPassword);

            if (command.ExecuteNonQuery() == 0)
            {
                throw new Exception("找不到要更新密碼的帳號。");
            }
        }

        public void CreateAccount(account user)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            EnsureMemberSchema(connection);

            using var command = new SqlCommand(@"
                INSERT INTO member(userName, password, age, email, phone, role, isActive)
                VALUES(@userName, @password, @age, @email, @phone, @role, @isActive)", connection);
            FillCreateParameters(command, user);
            command.ExecuteNonQuery();
        }

        public void UpdateAccount(account user)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            EnsureMemberSchema(connection);

            var updatePassword = !string.IsNullOrWhiteSpace(user.password);
            using var command = new SqlCommand(updatePassword
                ? @"
                    UPDATE member
                    SET password = @password,
                        age = @age,
                        email = @email,
                        phone = @phone
                    WHERE id = @id"
                : @"
                    UPDATE member
                    SET age = @age,
                        email = @email,
                        phone = @phone
                    WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", user.id);
            command.Parameters.AddWithValue("@age", user.age);
            command.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(user.email) ? DBNull.Value : user.email);
            command.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(user.phone) ? DBNull.Value : user.phone);
            if (updatePassword)
            {
                command.Parameters.AddWithValue("@password", user.password);
            }

            if (command.ExecuteNonQuery() == 0)
            {
                throw new Exception("找不到要更新的帳號。");
            }
        }

        public void ToggleAccountStatus(int id, bool isActive)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureConnectionOpen(connection);
            EnsureMemberSchema(connection);

            using var command = new SqlCommand(@"
                UPDATE member
                SET isActive = @isActive
                WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@isActive", isActive);

            if (command.ExecuteNonQuery() == 0)
            {
                throw new Exception("找不到要更新狀態的帳號。");
            }
        }

        private static void EnsureMemberSchema(SqlConnection connection)
        {
            EnsurePasswordColumnCapacity(connection);
            EnsureRoleColumn(connection);
            EnsureStatusColumn(connection);
        }

        private static void EnsurePasswordColumnCapacity(SqlConnection connection)
        {
            using var checkCommand = new SqlCommand("SELECT COL_LENGTH('member', 'password')", connection);
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

        private static void EnsureRoleColumn(SqlConnection connection)
        {
            using var checkCommand = new SqlCommand("SELECT COL_LENGTH('member', 'role')", connection);
            var exists = checkCommand.ExecuteScalar();
            if (exists == DBNull.Value || exists == null)
            {
                using var addCommand = new SqlCommand("ALTER TABLE member ADD role NVARCHAR(20) NOT NULL CONSTRAINT DF_member_role DEFAULT('user');", connection);
                addCommand.ExecuteNonQuery();
            }

            using var normalizeCommand = new SqlCommand(@"
                UPDATE member
                SET role = 'user'
                WHERE role IS NULL OR LTRIM(RTRIM(role)) = ''", connection);
            normalizeCommand.ExecuteNonQuery();
        }

        private static void EnsureStatusColumn(SqlConnection connection)
        {
            using var checkCommand = new SqlCommand("SELECT COL_LENGTH('member', 'isActive')", connection);
            var exists = checkCommand.ExecuteScalar();
            if (exists == DBNull.Value || exists == null)
            {
                using var addCommand = new SqlCommand("ALTER TABLE member ADD isActive BIT NOT NULL CONSTRAINT DF_member_isActive DEFAULT(1);", connection);
                addCommand.ExecuteNonQuery();
            }
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
                email = reader.IsDBNull(reader.GetOrdinal("email")) ? string.Empty : reader.GetString(reader.GetOrdinal("email")),
                phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? string.Empty : reader.GetString(reader.GetOrdinal("phone")),
                role = reader.IsDBNull(reader.GetOrdinal("role")) ? "user" : reader.GetString(reader.GetOrdinal("role")),
                isActive = reader.IsDBNull(reader.GetOrdinal("isActive")) || reader.GetBoolean(reader.GetOrdinal("isActive"))
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
                UsedAt = reader.IsDBNull(reader.GetOrdinal("UsedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UsedAt")),
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
                Status = string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status.Trim(),
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
                if (filter.Role == "manager-group")
                {
                    whereParts.Add("(role = 'manager' OR role = 'admin')");
                }
                else
                {
                    whereParts.Add("role = @role");
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                whereParts.Add("isActive = @isActive");
            }

            return whereParts;
        }

        private static void FillFilterParameters(SqlCommand command, AccountManagementFilter filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                command.Parameters.AddWithValue("@keyword", filter.ExactMatch ? filter.Keyword! : $"%{filter.Keyword}%");
            }

            if (!string.IsNullOrWhiteSpace(filter.Role) && filter.Role != "manager-group")
            {
                command.Parameters.AddWithValue("@role", filter.Role!);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                command.Parameters.AddWithValue("@isActive", filter.Status == "active");
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
            command.Parameters.AddWithValue("@isActive", user.isActive);
        }
    }
}
