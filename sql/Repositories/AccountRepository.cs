using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // AccountRepository 專心處理帳號模組的資料存取。
    // 這一輪除了既有的登入 / 帳號管理，也把忘記密碼需要的驗證碼資料流補進來。
    public class AccountRepository
    {
        private readonly DBmanager _dbManager;

        public AccountRepository(DBmanager dbManager)
        {
            _dbManager = dbManager;
        }

        public List<account> GetAllAccounts()
        {
            var accounts = new List<account>();

            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(
                "SELECT id, userName, password, age, email, phone, role FROM member ORDER BY id",
                connection);

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                accounts.Add(MapAccount(reader));
            }

            return accounts;
        }

        public account? ValidateUser(string username, string password)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                SELECT id, userName, password, age, email, phone, role
                FROM member
                WHERE userName = @userName AND password = @password",
                connection);

            command.Parameters.AddWithValue("@userName", username);
            command.Parameters.AddWithValue("@password", password);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapAccount(reader);
        }

        public account? GetAccountByEmail(string email)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                SELECT TOP 1 id, userName, password, age, email, phone, role
                FROM member
                WHERE email = @email",
                connection);

            command.Parameters.AddWithValue("@email", email);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapAccount(reader);
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

            connection.Open();
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapPasswordResetCode(reader);
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

            connection.Open();
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

            connection.Open();
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

            connection.Open();
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

            connection.Open();
            command.ExecuteNonQuery();
        }

        public void UpdatePasswordByUserId(int userId, string newPassword)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                UPDATE member
                SET password = @password
                WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", userId);
            command.Parameters.AddWithValue("@password", newPassword);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new Exception("找不到要更新密碼的帳號資料");
            }
        }

        public void CreateAccount(account user)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                INSERT INTO member(userName, password, age, email, phone, role)
                VALUES(@userName, @password, @age, @email, @phone, @role)",
                connection);

            FillCreateOrUpdateParameters(command, user, includeId: false);

            connection.Open();
            command.ExecuteNonQuery();
        }

        public void UpdateAccount(account user)
        {
            using var connection = _dbManager.CreateConnection();
            using var command = new SqlCommand(@"
                UPDATE member
                SET password = @password,
                    email = @email,
                    phone = @phone
                WHERE id = @id",
                connection);

            FillCreateOrUpdateParameters(command, user, includeId: true);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new Exception("找不到要更新的帳號資料");
            }
        }

        // 把資料庫欄位轉成 account 物件，讓上層不需要知道欄位細節。
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

        // 建立 / 更新帳號時，集中填入共用欄位。
        // 這樣 SQL 雖然不同，但欄位轉參數的規則不需要複製兩份。
        private static void FillCreateOrUpdateParameters(SqlCommand command, account user, bool includeId)
        {
            if (includeId)
            {
                command.Parameters.AddWithValue("@id", user.id);
            }

            if (command.CommandText.Contains("@userName"))
            {
                command.Parameters.AddWithValue("@userName", user.userName);
            }

            command.Parameters.AddWithValue("@password", string.IsNullOrEmpty(user.password) ? DBNull.Value : user.password);

            if (command.CommandText.Contains("@age"))
            {
                command.Parameters.AddWithValue("@age", user.age);
            }

            command.Parameters.AddWithValue("@email", string.IsNullOrEmpty(user.email) ? DBNull.Value : user.email);
            command.Parameters.AddWithValue("@phone", string.IsNullOrEmpty(user.phone) ? DBNull.Value : user.phone);

            if (command.CommandText.Contains("@role"))
            {
                command.Parameters.AddWithValue("@role", string.IsNullOrEmpty(user.role) ? "user" : user.role);
            }
        }
    }
}
