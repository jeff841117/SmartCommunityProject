using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

//using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace sql.Models
{
    public class DBmanager
    {
        // 正式環境連線字串請改放在本機私密設定或部署環境變數，不要直接寫在程式碼或註解中。
        // 添加靜態標誌防止循環調用
        //private static readonly Dictionary<byte, bool> _processingEquipment = new Dictionary<byte, bool>();
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=sql_db;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;";

        public List<account> getAccounts()
        {
            List<account> accounts = new List<account>();

            using (SqlConnection sqlConnection = new SqlConnection(connStr))
            using (SqlCommand sqlCommand = new SqlCommand("SELECT * FROM member", sqlConnection))
            {
                sqlConnection.Open();
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            account account = new account
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
                            accounts.Add(account);
                        }
                    }
                    else
                    {
                        Console.WriteLine("資料庫為空！");
                    }
                }
            }
            return accounts;
        }

        public void newAccount(account user)
        {
            using (SqlConnection sqlconnection = new SqlConnection(connStr))
            using (SqlCommand sqlcommand = new SqlCommand(@"INSERT INTO member(userName, password, age,email, phone, role) VALUES(@userName, @password, @age, @email, @phone, @role)", sqlconnection))
            {
                sqlcommand.Parameters.Add(new SqlParameter("@userName", user.userName));
                sqlcommand.Parameters.Add(new SqlParameter("@password", user.password));
                sqlcommand.Parameters.Add(new SqlParameter("@age", user.age));
                sqlcommand.Parameters.Add(new SqlParameter("@email",
            string.IsNullOrEmpty(user.email) ? DBNull.Value : (object)user.email));
                sqlcommand.Parameters.Add(new SqlParameter("@phone",
                    string.IsNullOrEmpty(user.phone) ? DBNull.Value : (object)user.phone));
                sqlcommand.Parameters.Add(new SqlParameter("@role",
                    string.IsNullOrEmpty(user.role) ? "user" : user.role));

                sqlconnection.Open();
                sqlcommand.ExecuteNonQuery();
            }
        }

        public void updateAccount(account user)
        {
            try
            {
                Console.WriteLine($"開始更新帳號 ID: {user.id}");
                Console.WriteLine($"密碼: {user.password}");
                Console.WriteLine($"Email: {user.email}");
                Console.WriteLine($"手機: {user.phone}");

                using (SqlConnection sqlconnection = new SqlConnection(connStr))
                using (SqlCommand sqlcommand = new SqlCommand(@"UPDATE member 
                                                       SET password = @password, 
                                                           email = @email, 
                                                           phone = @phone 
                                                       WHERE id = @id", sqlconnection))
                {
                    sqlcommand.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = user.id });

                    // 處理密碼參數
                    var passwordParam = new SqlParameter("@password", SqlDbType.NVarChar, 50);
                    passwordParam.Value = string.IsNullOrEmpty(user.password) ? DBNull.Value : (object)user.password;
                    sqlcommand.Parameters.Add(passwordParam);

                    // 處理 email 參數
                    var emailParam = new SqlParameter("@email", SqlDbType.NVarChar, 100);
                    emailParam.Value = string.IsNullOrEmpty(user.email) ? DBNull.Value : (object)user.email;
                    sqlcommand.Parameters.Add(emailParam);

                    // 處理 phone 參數
                    var phoneParam = new SqlParameter("@phone", SqlDbType.NVarChar, 20);
                    phoneParam.Value = string.IsNullOrEmpty(user.phone) ? DBNull.Value : (object)user.phone;
                    sqlcommand.Parameters.Add(phoneParam);

                    Console.WriteLine("連接字符串: " + connStr); // 注意：生產環境不要記錄完整連接字符串
                    sqlconnection.Open();
                    Console.WriteLine("資料庫連接成功");

                    int rowsAffected = sqlcommand.ExecuteNonQuery();
                    Console.WriteLine($"受影響的行數: {rowsAffected}");

                    if (rowsAffected == 0)
                    {
                        throw new Exception("沒有找到對應的帳號記錄");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新帳號時發生錯誤: {ex.Message}");
                Console.WriteLine($"堆棧跟踪: {ex.StackTrace}");
                throw; // 重新拋出異常讓控制器捕捉
            }
        }

        public account ValidateUser(string username, string password)
        {
            using (var connection = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("SELECT id, userName, password, age,email, phone, role FROM member WHERE userName = @userName AND password = @password", connection))
            {
                connection.Open();
                cmd.Parameters.AddWithValue("@UserName", username);
                cmd.Parameters.AddWithValue("@Password", password);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
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
                }
            }
            return null;
        }

        public List<Equipment> getEquipment()
        {
            List<Equipment> equipments = new List<Equipment>();

            using (SqlConnection sqlConnection = new SqlConnection(connStr))
            using (SqlCommand sqlCommand = new SqlCommand("SELECT * FROM Equipment", sqlConnection))
            {
                sqlConnection.Open();
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Equipment equipment = new Equipment
                            {
                                Id = reader.GetByte(reader.GetOrdinal("Id")),
                                equipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                                MaxUsers = reader.GetByte(reader.GetOrdinal("MaxUsers")),
                                AvailableTime = reader.GetInt16(reader.GetOrdinal("AvailableTime")),
                                OpenTime = reader.GetTimeSpan(reader.GetOrdinal("OpenTime")),
                                CloseTime = reader.GetTimeSpan(reader.GetOrdinal("CloseTime")),
                            };
                            equipments.Add(equipment);
                        }
                    }
                    else
                    {
                        Console.WriteLine("資料庫為空！");
                    }
                }
            }
            return equipments;
        }

        public void newEquipment(Equipment user)
        {
            using (SqlConnection sqlconnection = new SqlConnection(connStr))
            using (SqlCommand sqlcommand = new SqlCommand(@"INSERT INTO Equipment(equipmentName,MaxUsers,AvailableTime,OpenTime,CloseTime) 
        VALUES(@equipmentName,@MaxUsers,@AvailableTime,@OpenTime,@CloseTime)", sqlconnection))
            {
                sqlcommand.Parameters.Add(new SqlParameter("@equipmentName", SqlDbType.NVarChar) { Value = user.equipmentName });
                sqlcommand.Parameters.Add(new SqlParameter("@MaxUsers", SqlDbType.TinyInt) { Value = user.MaxUsers });
                sqlcommand.Parameters.Add(new SqlParameter("@AvailableTime", SqlDbType.SmallInt) { Value = user.AvailableTime });
                sqlcommand.Parameters.Add(new SqlParameter("@OpenTime", SqlDbType.Time) { Value = user.OpenTime });
                sqlcommand.Parameters.Add(new SqlParameter("@CloseTime", SqlDbType.Time) { Value = user.CloseTime });

                sqlconnection.Open();
                sqlcommand.ExecuteNonQuery();
            }
        }

        public void deleteEquipment(byte id)
        {
            SqlConnection sqlconnection = new SqlConnection(connStr);
            SqlCommand sqlcommand = new SqlCommand(@"DELETE FROM Equipment WHERE Id = @Id");
            sqlcommand.Connection = sqlconnection;

            sqlcommand.Parameters.Add(new SqlParameter("@Id", SqlDbType.TinyInt) { Value = id });

            sqlconnection.Open();
            sqlcommand.ExecuteNonQuery();
            sqlconnection.Close();
        }

        public void updateEquipment(Equipment equipment)
        {
            SqlConnection sqlconnection = new SqlConnection(connStr);
            SqlCommand sqlcommand = new SqlCommand(@"UPDATE Equipment SET equipmentName = @equipmentName, MaxUsers = @MaxUsers, 
            AvailableTime = @AvailableTime, OpenTime = @OpenTime, CloseTime = @CloseTime WHERE Id = @Id");
            sqlcommand.Connection = sqlconnection;

            sqlcommand.Parameters.Add(new SqlParameter("@equipmentName", SqlDbType.NVarChar) { Value = equipment.equipmentName });
            sqlcommand.Parameters.Add(new SqlParameter("@MaxUsers", SqlDbType.TinyInt) { Value = equipment.MaxUsers });
            sqlcommand.Parameters.Add(new SqlParameter("@AvailableTime", SqlDbType.SmallInt) { Value = equipment.AvailableTime });
            sqlcommand.Parameters.Add(new SqlParameter("@OpenTime", SqlDbType.Time) { Value = equipment.OpenTime });
            sqlcommand.Parameters.Add(new SqlParameter("@CloseTime", SqlDbType.Time) { Value = equipment.CloseTime });
            sqlcommand.Parameters.Add(new SqlParameter("@Id", SqlDbType.TinyInt) { Value = equipment.Id });

            sqlconnection.Open();
            sqlcommand.ExecuteNonQuery();
            sqlconnection.Close();
        }

        // 新增預約相關方法

        // 創建預約
        public ReservationResult CreateReservation(byte equipmentId, string userId)
        {
            try
            {
                var equipment = GetEquipmentById(equipmentId);
                if (equipment == null)
                {
                    return new ReservationResult { Success = false, Message = "設備不存在" };
                }

                DateTime taiwanTime = GetTaiwanTime();

                // 檢查是否已經有進行中的預約或排隊
                if (HasActiveReservationForSameEquipment(equipmentId, userId))
                {
                    return new ReservationResult { Success = false, Message = "您在此設備已有進行中的預約，無法重複預約" };
                }

                // 檢查是否已在排隊中 - 加強檢查
                if (IsUserInWaitingQueue(equipmentId, userId))
                {
                    return new ReservationResult { Success = false, Message = "您已在此設備的排隊隊伍中，請耐心等候" };
                }

                // 檢查開放時間
                if (!IsWithinOperatingHours(equipment, taiwanTime))
                {
                    return new ReservationResult { Success = false, Message = "不在設備開放時間內" };
                }

                // 獲取當前使用人數
                int currentUsers = GetCurrentUsers(equipmentId);

                if (currentUsers < equipment.MaxUsers)
                {
                    // 直接開始使用
                    var reservation = new Reservation
                    {
                        EquipmentId = equipmentId,
                        UserId = userId,
                        StartTime = taiwanTime,
                        ReservationTime = taiwanTime,
                        Status = ReservationStatus.InProgress
                    };

                    InsertReservation(reservation);
                    NotifyEquipmentStateChanged(equipmentId);

                    return new ReservationResult { Success = true, Message = "預約成功，立即開始使用" };
                }
                else
                {
                    // 進入排隊
                    int queuePosition = AddToWaitingQueue(equipmentId, userId);
                    int estimatedWaitTime = CalculateEstimatedWaitTime(equipmentId, queuePosition);
                    DateTime expectedStartTime = taiwanTime.AddMinutes(estimatedWaitTime);

                    return new ReservationResult
                    {
                        Success = true,
                        Message = "設備已滿，已加入排隊",
                        WaitingPosition = queuePosition,
                        EstimatedWaitTime = estimatedWaitTime,
                        ExpectedStartTime = expectedStartTime
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"創建預約時發生錯誤: {ex.Message}");
                return new ReservationResult { Success = false, Message = "系統錯誤，請稍後再試" };
            }
        }
        //public ReservationResult CreateReservation(byte equipmentId, string userId)
        //{
        //    try
        //    {
        //        var equipment = GetEquipmentById(equipmentId);
        //        if (equipment == null)
        //        {
        //            return new ReservationResult
        //            {
        //                Success = false,
        //                Message = "設備不存在"
        //            };
        //        }

        //        // 使用台灣時間
        //        DateTime taiwanTime = GetTaiwanTime();

        //        Console.WriteLine($"=== 預約檢查開始 ===");
        //        Console.WriteLine($"台灣時間: {taiwanTime:yyyy-MM-dd HH:mm:ss}");
        //        Console.WriteLine($"設備: {equipment.equipmentName}");
        //        Console.WriteLine($"用戶: {userId}");

        //        // 檢查是否已經有進行中的預約
        //        if (HasActiveReservationForSameEquipment(equipmentId, userId))
        //        {
        //            Console.WriteLine($"預約失敗: 用戶已有進行中的預約");
        //            return new ReservationResult
        //            {
        //                Success = false,
        //                Message = "您在此設備已有進行中的預約，無法重複預約"
        //            };
        //        }

        //        // 檢查是否在開放時間內
        //        if (!IsWithinOperatingHours(equipment, taiwanTime))
        //        {
        //            var message = $"不在設備開放時間內（開放時間: {equipment.OpenTime:hh\\:mm} - {equipment.CloseTime:hh\\:mm}，當前台灣時間: {taiwanTime:HH:mm}）";
        //            Console.WriteLine($"預約失敗: {message}");
        //            return new ReservationResult
        //            {
        //                Success = false,
        //                Message = message
        //            };
        //        }

        //        // 獲取當前使用人數（不觸發排隊處理）
        //        int currentUsers = GetCurrentUsers(equipmentId);
        //        Console.WriteLine($"當前使用人數: {currentUsers}/{equipment.MaxUsers}");

        //        if (currentUsers < equipment.MaxUsers)
        //        {
        //            // 直接開始使用
        //            var reservation = new Reservation
        //            {
        //                EquipmentId = equipmentId,
        //                UserId = userId,
        //                StartTime = taiwanTime,
        //                ReservationTime = taiwanTime,
        //                Status = ReservationStatus.InProgress
        //            };

        //            InsertReservation(reservation);
        //            Console.WriteLine($"預約成功: 立即開始使用");

        //            // 通知狀態改變
        //            NotifyEquipmentStateChanged(equipmentId);

        //            return new ReservationResult
        //            {
        //                Success = true,
        //                Message = "預約成功，立即開始使用"
        //            };
        //        }
        //        else
        //        {
        //            // 檢查是否已經在排隊中
        //            if (IsUserInWaitingQueue(equipmentId, userId))
        //            {
        //                Console.WriteLine($"預約失敗: 用戶已在排隊中");
        //                return new ReservationResult
        //                {
        //                    Success = false,
        //                    Message = "您已在此設備的排隊隊伍中，請耐心等候"
        //                };
        //            }

        //            // 進入排隊
        //            int queuePosition = AddToWaitingQueue(equipmentId, userId);
        //            int estimatedWaitTime = CalculateEstimatedWaitTime(equipmentId, queuePosition);

        //            // 使用台灣時間計算預計開始時間
        //            DateTime expectedStartTime = taiwanTime.AddMinutes(estimatedWaitTime);

        //            Console.WriteLine($"進入排隊: 位置 {queuePosition}, 預計等待 {estimatedWaitTime} 分鐘");

        //            return new ReservationResult
        //            {
        //                Success = true,
        //                Message = "設備已滿，已加入排隊",
        //                WaitingPosition = queuePosition,
        //                EstimatedWaitTime = estimatedWaitTime,
        //                ExpectedStartTime = expectedStartTime
        //            };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"創建預約時發生錯誤: {ex.Message}");
        //        return new ReservationResult
        //        {
        //            Success = false,
        //            Message = "系統錯誤，請稍後再試"
        //        };
        //    }
        //}


        // 添加自動結束過期預約的方法
        public void AutoCompleteExpiredReservations()
        {
            try
            {
                using (var connection = new SqlConnection(connStr))
                {
                    connection.Open();

                    // 獲取所有過期的進行中預約
                    var selectCmd = new SqlCommand(@"
                SELECT r.Id, r.EquipmentId, r.StartTime, r.UserId, e.AvailableTime
                FROM Reservations r
                INNER JOIN Equipment e ON r.EquipmentId = e.Id
                WHERE r.Status = @Status 
                AND DATEADD(MINUTE, e.AvailableTime, r.StartTime) <= @CurrentTime",
                        connection);
                    selectCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);
                    selectCmd.Parameters.AddWithValue("@CurrentTime", GetTaiwanTime());

                    var expiredReservations = new List<ExpiredReservation>();
                    var reader = selectCmd.ExecuteReader();

                    while (reader.Read())
                    {
                        try
                        {
                            var id = reader.IsDBNull(reader.GetOrdinal("Id")) ? 0 : reader.GetInt32(reader.GetOrdinal("Id"));
                            var equipmentId = reader.IsDBNull(reader.GetOrdinal("EquipmentId")) ? (byte)0 : reader.GetByte(reader.GetOrdinal("EquipmentId"));
                            var userId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? string.Empty : reader.GetString(reader.GetOrdinal("UserId"));
                            var availableTime = reader.IsDBNull(reader.GetOrdinal("AvailableTime")) ? (short)0 : reader.GetInt16(reader.GetOrdinal("AvailableTime"));

                            expiredReservations.Add(new ExpiredReservation
                            {
                                Id = id,
                                EquipmentId = equipmentId,
                                UserId = userId,
                                AvailableTime = availableTime
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"讀取過期預約數據時錯誤: {ex.Message}");
                            continue;
                        }
                    }
                    reader.Close();

                    // 批量更新過期預約狀態
                    if (expiredReservations.Any())
                    {
                        foreach (var reservation in expiredReservations)
                        {
                            try
                            {
                                var updateCmd = new SqlCommand(@"
                            UPDATE Reservations 
                            SET Status = @CompletedStatus, EndTime = @EndTime
                            WHERE Id = @Id",
                                    connection);
                                updateCmd.Parameters.AddWithValue("@CompletedStatus", (int)ReservationStatus.Completed);
                                updateCmd.Parameters.AddWithValue("@EndTime", GetTaiwanTime());
                                updateCmd.Parameters.AddWithValue("@Id", reservation.Id);
                                updateCmd.ExecuteNonQuery();

                                Console.WriteLine($"自動結束過期預約: 預約ID {reservation.Id}, 設備ID {reservation.EquipmentId}");

                                // 處理排隊隊列 - 新增這行
                                ProcessWaitingQueue(reservation.EquipmentId);

                                // 通知狀態改變
                                NotifyEquipmentStateChanged(reservation.EquipmentId);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"更新過期預約時錯誤 (ID: {reservation.Id}): {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AutoCompleteExpiredReservations 整體錯誤: {ex.Message}");
            }
        }

        private class ExpiredReservation
        {
            public int Id { get; set; }
            public byte EquipmentId { get; set; }
            public string UserId { get; set; }
            public short AvailableTime { get; set; } // 修正：改為正確的類型
        }

        // 檢查用戶是否在相同設備有進行中的預約
        private bool HasActiveReservationForSameEquipment(byte equipmentId, string userId)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM Reservations 
            WHERE EquipmentId = @EquipmentId 
            AND UserId = @UserId 
            AND Status = @Status",
                    connection);
                cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        // 檢查用戶是否已在排隊中
        private bool IsUserInWaitingQueue(byte equipmentId, string userId)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();
                var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM WaitingQueue WHERE EquipmentId = @EquipmentId AND UserId = @UserId",
                    connection);
                cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        //private bool IsUserInWaitingQueue(byte equipmentId, string userId)
        //{
        //    using (var connection = new SqlConnection(connStr))
        //    {
        //        connection.Open();

        //        var cmd = new SqlCommand(@"
        //    SELECT COUNT(*) FROM WaitingQueue 
        //    WHERE EquipmentId = @EquipmentId 
        //    AND UserId = @UserId",
        //            connection);
        //        cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
        //        cmd.Parameters.AddWithValue("@UserId", userId);

        //        int count = (int)cmd.ExecuteScalar();
        //        return count > 0;
        //    }
        //}

        // 取消預約
        public bool CancelReservation(int reservationId, string userId)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                // 檢查預約是否存在且屬於該用戶
                var checkCmd = new SqlCommand(
                    "SELECT Status FROM Reservations WHERE Id = @Id AND UserId = @UserId",
                    connection);
                checkCmd.Parameters.AddWithValue("@Id", reservationId);
                checkCmd.Parameters.AddWithValue("@UserId", userId);

                var result = checkCmd.ExecuteScalar();
                if (result == null) return false;

                var status = (ReservationStatus)result;

                // 更新預約狀態為取消
                var updateCmd = new SqlCommand(
                    "UPDATE Reservations SET Status = @Status, EndTime = GETDATE() WHERE Id = @Id",
                    connection);
                updateCmd.Parameters.AddWithValue("@Status", ReservationStatus.Cancelled);
                updateCmd.Parameters.AddWithValue("@Id", reservationId);

                int rowsAffected = updateCmd.ExecuteNonQuery();

                if (rowsAffected > 0 && status == ReservationStatus.InProgress)
                {
                    // 如果取消的是正在使用的預約，觸發觀察者更新
                    var reservation = GetReservationById(reservationId);
                    if (reservation != null)
                    {
                        NotifyEquipmentStateChanged(reservation.EquipmentId);
                    }
                }

                return rowsAffected > 0;
            }
        }

        // 結束使用
        public bool EndUsage(int reservationId, string userId)
        {
            try
            {
                using (var connection = new SqlConnection(connStr))
                {
                    connection.Open();

                    // 檢查預約
                    var checkCmd = new SqlCommand(
                        "SELECT EquipmentId FROM Reservations WHERE Id = @Id AND UserId = @UserId AND Status = @Status",
                        connection);
                    checkCmd.Parameters.AddWithValue("@Id", reservationId);
                    checkCmd.Parameters.AddWithValue("@UserId", userId);
                    checkCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

                    var result = checkCmd.ExecuteScalar();
                    if (result == null) return false;

                    byte equipmentId = (byte)result;

                    // 結束預約
                    var updateCmd = new SqlCommand(
                        "UPDATE Reservations SET Status = @Status, EndTime = @EndTime WHERE Id = @Id",
                        connection);
                    updateCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Completed);
                    updateCmd.Parameters.AddWithValue("@EndTime", GetTaiwanTime());
                    updateCmd.Parameters.AddWithValue("@Id", reservationId);

                    if (updateCmd.ExecuteNonQuery() > 0)
                    {
                        // 立即處理排隊
                        ProcessWaitingQueue(equipmentId);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"結束使用錯誤: {ex.Message}");
                return false;
            }
        }
        //public bool EndUsage(int reservationId, string userId)
        //{
        //    try
        //    {
        //        using (var connection = new SqlConnection(connStr))
        //        {
        //            connection.Open();

        //            Console.WriteLine($"=== 執行結束使用 ===");
        //            Console.WriteLine($"預約ID: {reservationId}");
        //            Console.WriteLine($"用戶ID: {userId}");

        //            // 先檢查預約是否存在且屬於該用戶
        //            var checkCmd = new SqlCommand(
        //                "SELECT EquipmentId, Status FROM Reservations WHERE Id = @Id AND UserId = @UserId",
        //                connection);
        //            checkCmd.Parameters.AddWithValue("@Id", reservationId);
        //            checkCmd.Parameters.AddWithValue("@UserId", userId);

        //            var reader = checkCmd.ExecuteReader();
        //            if (!reader.Read())
        //            {
        //                reader.Close();
        //                Console.WriteLine("預約不存在或不屬於該用戶");
        //                return false;
        //            }

        //            var equipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId"));
        //            var status = (ReservationStatus)reader.GetInt32(reader.GetOrdinal("Status"));
        //            reader.Close();

        //            Console.WriteLine($"設備ID: {equipmentId}, 當前狀態: {status}");

        //            if (status != ReservationStatus.InProgress)
        //            {
        //                Console.WriteLine($"預約狀態不是進行中，當前狀態: {status}");
        //                return false;
        //            }

        //            // 更新預約狀態為已完成
        //            var updateCmd = new SqlCommand(
        //                "UPDATE Reservations SET Status = @Status, EndTime = @EndTime WHERE Id = @Id AND UserId = @UserId AND Status = @CurrentStatus",
        //                connection);
        //            updateCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Completed);
        //            updateCmd.Parameters.AddWithValue("@EndTime", GetTaiwanTime());
        //            updateCmd.Parameters.AddWithValue("@Id", reservationId);
        //            updateCmd.Parameters.AddWithValue("@UserId", userId);
        //            updateCmd.Parameters.AddWithValue("@CurrentStatus", (int)ReservationStatus.InProgress);

        //            int rowsAffected = updateCmd.ExecuteNonQuery();
        //            Console.WriteLine($"更新影響行數: {rowsAffected}");

        //            if (rowsAffected > 0)
        //            {
        //                // 處理排隊隊列 - 新增這行
        //                ProcessWaitingQueue(equipmentId);

        //                // 通知設備狀態改變
        //                NotifyEquipmentStateChanged(equipmentId);
        //                Console.WriteLine($"結束使用成功，已通知設備 {equipmentId} 狀態更新");
        //                return true;
        //            }
        //            else
        //            {
        //                Console.WriteLine("沒有更新任何行，可能預約狀態已改變");
        //                return false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"結束使用數據庫錯誤: {ex}");
        //        return false;
        //    }
        //}

        // 獲取設備當前使用人數，自動檢查過期預約
        public int GetCurrentUsers(byte equipmentId)
        {
            using (var connection = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Reservations WHERE EquipmentId = @EquipmentId AND Status = @Status",
                connection))
            {
                cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
                cmd.Parameters.AddWithValue("@Status", ReservationStatus.InProgress);

                connection.Open();
                return (int)cmd.ExecuteScalar();
            }
        }


        // 添加排隊推進方法
        //public void ProcessAllWaitingQueues()
        //{
        //    try
        //    {
        //        Console.WriteLine("=== 開始處理所有設備的排隊隊列 ===");

        //        // 先處理所有過期預約
        //        AutoCompleteExpiredReservations();

        //        // 獲取所有設備
        //        var equipments = getEquipment();
        //        Console.WriteLine($"找到 {equipments.Count} 個設備需要處理排隊");

        //        foreach (var equipment in equipments)
        //        {
        //            try
        //            {
        //                Console.WriteLine($"處理設備 {equipment.equipmentName} (ID: {equipment.Id}) 的排隊隊列");
        //                ProcessWaitingQueue(equipment.Id);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"處理設備 {equipment.Id} 排隊時錯誤: {ex.Message}");
        //            }
        //        }

        //        Console.WriteLine("=== 所有設備排隊隊列處理完成 ===");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"處理所有排隊隊列時錯誤: {ex.Message}");
        //    }
        //}

        // 添加自動推進排隊的方法  修正 ProcessWaitingQueue 方法，防止循環調用
        public void ProcessWaitingQueue(byte equipmentId)
        {
            try
            {
                using (var connection = new SqlConnection(connStr))
                {
                    connection.Open();

                    // 完成過期預約
                    AutoCompleteExpiredReservations();

                    var equipment = GetEquipmentById(equipmentId);
                    if (equipment == null) return;

                    // 使用現有方法獲取數據
                    int currentUsers = GetCurrentUsers(equipmentId);
                    var waitingList = GetWaitingQueue(equipmentId);
                    int queueCount = waitingList.Count;

                    Console.WriteLine($"處理排隊: 設備{equipmentId}, 使用中{currentUsers}/{equipment.MaxUsers}, 排隊{queueCount}人");

                    // 處理排隊
                    while (currentUsers < equipment.MaxUsers && waitingList.Count > 0)
                    {
                        var nextInQueue = waitingList[0]; // 取第一個

                        if (!HasActiveReservationForSameEquipment(equipmentId, nextInQueue.UserId))
                        {
                            // 創建預約
                            var reservation = new Reservation
                            {
                                EquipmentId = equipmentId,
                                UserId = nextInQueue.UserId,
                                StartTime = GetTaiwanTime(),
                                ReservationTime = GetTaiwanTime(),
                                Status = ReservationStatus.InProgress
                            };

                            InsertReservation(reservation, connection);
                            Console.WriteLine($"排隊用戶 {nextInQueue.UserId} 開始使用設備");
                            currentUsers++;
                        }

                        // 從排隊中移除
                        RemoveFromQueue(nextInQueue.Id, connection);

                        // 重新獲取排隊列表
                        waitingList = GetWaitingQueue(equipmentId);

                        // 通知
                        NotifyEquipmentStateChanged(equipmentId);
                        NotifyUserPromotedFromQueue(nextInQueue.UserId, equipment.equipmentName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"處理排隊錯誤: {ex.Message}");
            }
        }
        //public void ProcessWaitingQueue(byte equipmentId)
        //{
        //    try
        //    {
        //        using (var connection = new SqlConnection(connStr))
        //        {
        //            connection.Open();

        //            // 先完成過期預約
        //            AutoCompleteExpiredReservationsForEquipment(equipmentId, connection);

        //            var equipment = GetEquipmentById(equipmentId);
        //            if (equipment == null) return;

        //            int currentUsers = GetCurrentUsersForEquipment(equipmentId, connection);
        //            int queueCount = GetWaitingQueueCount(equipmentId, connection);

        //            Console.WriteLine($"處理排隊: 設備{equipmentId}, 使用中{currentUsers}/{equipment.MaxUsers}, 排隊{queueCount}人");

        //            // 處理排隊
        //            while (currentUsers < equipment.MaxUsers && queueCount > 0)
        //            {
        //                var nextInQueue = GetNextInQueue(equipmentId, connection);
        //                if (nextInQueue == null) break;

        //                // 再次檢查用戶是否已有預約
        //                if (!HasActiveReservationForSameEquipment(equipmentId, nextInQueue.UserId))
        //                {
        //                    // 創建預約
        //                    var reservation = new Reservation
        //                    {
        //                        EquipmentId = equipmentId,
        //                        UserId = nextInQueue.UserId,
        //                        StartTime = GetTaiwanTime(),
        //                        ReservationTime = GetTaiwanTime(),
        //                        Status = ReservationStatus.InProgress
        //                    };

        //                    InsertReservation(reservation, connection);
        //                    Console.WriteLine($"排隊用戶 {nextInQueue.UserId} 開始使用設備");
        //                }

        //                // 從排隊中移除
        //                RemoveFromQueue(nextInQueue.Id, connection);

        //                // 更新計數
        //                currentUsers++;
        //                queueCount--;

        //                // 重新計算排隊位置
        //                RecalculateQueuePositions(equipmentId, connection);

        //                // 通知
        //                NotifyEquipmentStateChanged(equipmentId);
        //                NotifyUserPromotedFromQueue(nextInQueue.UserId, equipment.equipmentName);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"處理排隊錯誤: {ex.Message}");
        //    }
        //}
        //public void ProcessWaitingQueue(byte equipmentId)
        //{
        //    // 檢查是否正在處理該設備，防止循環調用
        //    if (_processingEquipment.ContainsKey(equipmentId) && _processingEquipment[equipmentId])
        //    {
        //        Console.WriteLine($"設備 {equipmentId} 正在處理中，跳過");
        //        return;
        //    }

        //    try
        //    {
        //        _processingEquipment[equipmentId] = true;

        //        using (var connection = new SqlConnection(connStr))
        //        {
        //            connection.Open();

        //            // 先處理該設備的過期預約
        //            //AutoCompleteExpiredReservationsForEquipment(equipmentId, connection);

        //            // 檢查設備當前使用人數
        //            int currentUsers = GetCurrentUsersForEquipment(equipmentId, connection);
        //            var equipment = GetEquipmentById(equipmentId);

        //            if (equipment == null)
        //            {
        //                Console.WriteLine($"設備 {equipmentId} 不存在");
        //                return;
        //            }

        //            Console.WriteLine($"=== 處理設備 {equipment.equipmentName} 的排隊隊列 ===");
        //            Console.WriteLine($"當前使用人數: {currentUsers}/{equipment.MaxUsers}");

        //            // 檢查是否有排隊記錄
        //            int queueCount = GetWaitingQueueCount(equipmentId, connection);
        //            Console.WriteLine($"當前排隊人數: {queueCount}");

        //            // 如果設備還有空位且有人排隊，處理排隊
        //            while (currentUsers < equipment.MaxUsers && queueCount > 0)
        //            {
        //                // 獲取排隊隊列中的第一個用戶
        //                var nextInQueue = GetNextInQueue(equipmentId, connection);
        //                if (nextInQueue == null)
        //                {
        //                    Console.WriteLine("沒有找到排隊用戶");
        //                    break;
        //                }

        //                Console.WriteLine($"處理排隊用戶: {nextInQueue.UserId}, 位置: {nextInQueue.Position}");

        //                // 檢查用戶是否已經有進行中的預約（防止重複）
        //                if (HasActiveReservationForSameEquipment(equipmentId, nextInQueue.UserId))
        //                {
        //                    Console.WriteLine($"用戶 {nextInQueue.UserId} 已有進行中的預約，跳過並移除排隊");
        //                    RemoveFromQueue(nextInQueue.Id, connection);
        //                    continue;
        //                }

        //                // 創建預約記錄
        //                var reservation = new Reservation
        //                {
        //                    EquipmentId = equipmentId,
        //                    UserId = nextInQueue.UserId,
        //                    StartTime = GetTaiwanTime(),
        //                    ReservationTime = GetTaiwanTime(),
        //                    Status = ReservationStatus.InProgress
        //                };

        //                // 插入預約記錄
        //                InsertReservation(reservation, connection);

        //                // 從排隊隊列中刪除
        //                RemoveFromQueue(nextInQueue.Id, connection);

        //                Console.WriteLine($"用戶 {nextInQueue.UserId} 已從排隊轉為使用中");

        //                // 更新當前使用人數和排隊人數
        //                currentUsers++;
        //                queueCount--;

        //                // 重新計算排隊位置
        //                RecalculateQueuePositions(equipmentId, connection);

        //                // 通知用戶
        //                NotifyUserPromotedFromQueue(nextInQueue.UserId, equipment.equipmentName);

        //                // 通知狀態改變
        //                NotifyEquipmentStateChanged(equipmentId);
        //            }

        //            if (currentUsers < equipment.MaxUsers)
        //            {
        //                Console.WriteLine($"設備 {equipment.equipmentName} 仍有空位: {currentUsers}/{equipment.MaxUsers}");
        //            }
        //            else
        //            {
        //                Console.WriteLine($"設備 {equipment.equipmentName} 已滿: {currentUsers}/{equipment.MaxUsers}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"處理設備 {equipmentId} 的排隊隊列錯誤: {ex.Message}");
        //    }
        //    finally
        //    {
        //        _processingEquipment[equipmentId] = false;
        //    }
        //}

        // 專門為設備獲取當前使用人數（不觸發排隊處理）
        //private int GetCurrentUsersForEquipment(byte equipmentId, SqlConnection connection)
        //{
        //    var cmd = new SqlCommand(
        //        "SELECT COUNT(*) FROM Reservations WHERE EquipmentId = @EquipmentId AND Status = @Status",
        //        connection);
        //    cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
        //    cmd.Parameters.AddWithValue("@Status", ReservationStatus.InProgress);

        //    return (int)cmd.ExecuteScalar();
        //}

        // 獲取排隊人數
        //private int GetWaitingQueueCount(byte equipmentId, SqlConnection connection)
        //{
        //    var cmd = new SqlCommand(
        //        "SELECT COUNT(*) FROM WaitingQueue WHERE EquipmentId = @EquipmentId",
        //        connection);
        //    cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

        //    return (int)cmd.ExecuteScalar();
        //}

        // 為單個設備處理過期預約
        //private void AutoCompleteExpiredReservationsForEquipment(byte equipmentId, SqlConnection connection)
        //{
        //    try
        //    {
        //        var cmd = new SqlCommand(@"
        //        UPDATE Reservations 
        //        SET Status = @CompletedStatus, EndTime = @CurrentTime
        //        WHERE EquipmentId = @EquipmentId 
        //        AND Status = @InProgressStatus 
        //        AND DATEADD(MINUTE, (SELECT AvailableTime FROM Equipment WHERE Id = @EquipmentId), StartTime) <= @CurrentTime",
        //            connection);

        //        cmd.Parameters.AddWithValue("@CompletedStatus", (int)ReservationStatus.Completed);
        //        cmd.Parameters.AddWithValue("@CurrentTime", GetTaiwanTime());
        //        cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
        //        cmd.Parameters.AddWithValue("@InProgressStatus", (int)ReservationStatus.InProgress);

        //        int rowsAffected = cmd.ExecuteNonQuery();
        //        if (rowsAffected > 0)
        //        {
        //            Console.WriteLine($"自動完成設備 {equipmentId} 的 {rowsAffected} 個過期預約");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"處理設備 {equipmentId} 的過期預約時錯誤: {ex.Message}");
        //    }
        //}

        // 添加調試方法，檢查排隊狀態
        //public Dictionary<string, object> GetQueueDebugInfo(byte equipmentId)
        //{
        //    var debugInfo = new Dictionary<string, object>();

        //    using (var connection = new SqlConnection(connStr))
        //    {
        //        connection.Open();

        //        // 獲取設備信息
        //        var equipment = GetEquipmentById(equipmentId);
        //        debugInfo["Equipment"] = equipment?.equipmentName ?? "未知設備";

        //        // 獲取當前使用人數
        //        int currentUsers = GetCurrentUsersForEquipment(equipmentId, connection);
        //        debugInfo["CurrentUsers"] = currentUsers;
        //        debugInfo["MaxUsers"] = equipment?.MaxUsers ?? 0;

        //        // 獲取排隊信息
        //        var queueCmd = new SqlCommand(
        //            "SELECT COUNT(*) as QueueCount, MIN(Position) as MinPosition, MAX(Position) as MaxPosition FROM WaitingQueue WHERE EquipmentId = @EquipmentId",
        //            connection);
        //        queueCmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

        //        var reader = queueCmd.ExecuteReader();
        //        if (reader.Read())
        //        {
        //            debugInfo["QueueCount"] = reader["QueueCount"];
        //            debugInfo["MinPosition"] = reader["MinPosition"];
        //            debugInfo["MaxPosition"] = reader["MaxPosition"];
        //        }
        //        reader.Close();

        //        debugInfo["HasVacancy"] = currentUsers < (equipment?.MaxUsers ?? 0);
        //        debugInfo["CanProcessQueue"] = currentUsers < (equipment?.MaxUsers ?? 0) && (int)debugInfo["QueueCount"] > 0;
        //    }

        //    return debugInfo;
        //}


        private void NotifyUserPromotedFromQueue(string userId, string equipmentName)
        {
            try
            {
                Console.WriteLine($"🎉 通知: 用戶 {userId} 已從 {equipmentName} 的排隊中晉升為使用中");
                Console.WriteLine($"📢 請立即前往使用設備: {equipmentName}");

                // 這裡可以擴展為其他通知方式：
                // - 發送電子郵件
                // - 發送短信
                // - 推送通知到前端
                // - 記錄到數據庫通知表

                // 示例：記錄到數據庫通知表（如果有的話）
                // LogNotificationToDatabase(userId, $"您已從 {equipmentName} 的排隊中晉升，請立即前往使用");

                // 示例：發送到前端 WebSocket 通知（如果有的話）
                // _notificationHub.Clients.User(userId).SendAsync("QueuePromoted", equipmentName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"通知用戶錯誤: {ex.Message}");
            }
        }

        // 在程式啟動時添加定時排隊處理
        public void StartQueueProcessingService()
        {
            var timer = new System.Timers.Timer(30000); // 每30秒執行一次
            timer.Elapsed += (sender, e) => {
                try
                {
                    var equipments = getEquipment();
                    foreach (var equipment in equipments)
                    {
                        ProcessWaitingQueue(equipment.Id);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"定時排隊處理錯誤: {ex.Message}");
                }
            };
            timer.Start();
        }

        // 獲取排隊隊列中的第一個用戶
        private WaitingQueue GetNextInQueue(byte equipmentId, SqlConnection connection)
        {
            var cmd = new SqlCommand(
                "SELECT TOP 1 * FROM WaitingQueue WHERE EquipmentId = @EquipmentId ORDER BY Position",
                connection);
            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new WaitingQueue
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    QueueTime = reader.GetDateTime(reader.GetOrdinal("QueueTime")),
                    Position = reader.GetInt32(reader.GetOrdinal("Position"))
                };
            }
            reader.Close();
            return null;
        }

        // 從排隊隊列中刪除
        private void RemoveFromQueue(int queueId, SqlConnection connection)
        {
            var cmd = new SqlCommand("DELETE FROM WaitingQueue WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", queueId);
            cmd.ExecuteNonQuery();
        }

        // 獲取排隊信息
        public List<WaitingQueue> GetWaitingQueue(byte equipmentId)
        {
            var queue = new List<WaitingQueue>();

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var cmd = new SqlCommand(
                    "SELECT * FROM WaitingQueue WHERE EquipmentId = @EquipmentId ORDER BY QueueTime",
                    connection);
                cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    queue.Add(new WaitingQueue
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                        QueueTime = reader.GetDateTime(reader.GetOrdinal("QueueTime")),
                        Position = reader.GetInt32(reader.GetOrdinal("Position"))
                    });
                }
            }

            return queue;
        }

        // 添加用戶到排隊隊列
        private int AddToWaitingQueue(byte equipmentId, string userId)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                // 獲取當前排隊人數
                var countCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM WaitingQueue WHERE EquipmentId = @EquipmentId",
                    connection);
                countCmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

                int queueCount = (int)countCmd.ExecuteScalar();
                int position = queueCount + 1;

                // 使用正確的北京時間
                DateTime queueTime = GetTaiwanTime();

                // 插入排隊記錄
                var insertCmd = new SqlCommand(
                    "INSERT INTO WaitingQueue (EquipmentId, UserId, QueueTime, Position) VALUES (@EquipmentId, @UserId, @QueueTime, @Position)",
                    connection);
                insertCmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
                insertCmd.Parameters.AddWithValue("@UserId", userId);
                insertCmd.Parameters.AddWithValue("@QueueTime", queueTime);
                insertCmd.Parameters.AddWithValue("@Position", position);

                insertCmd.ExecuteNonQuery();

                Console.WriteLine($"用戶 {userId} 加入排隊，時間: {queueTime:yyyy-MM-dd HH:mm:ss}");

                return position;
            }
        }

        // 計算預計等待時間
        private int CalculateEstimatedWaitTime(byte equipmentId, int queuePosition)
        {
            var equipment = GetEquipmentById(equipmentId);
            // 簡單計算：排隊位置 * 平均使用時間
            return queuePosition * (equipment.AvailableTime / 60); // 轉換為分鐘
        }

        // 檢查是否在開放時間內
        private bool IsWithinOperatingHours(Equipment equipment, DateTime currentTime)
        {
            try
            {
                // 使用台灣時間
                DateTime taiwanTime = GetTaiwanTime();
                TimeSpan currentTimeOfDay = taiwanTime.TimeOfDay;

                Console.WriteLine($"台灣當前時間: {taiwanTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"設備開放時間: {equipment.OpenTime:hh\\:mm} - {equipment.CloseTime:hh\\:mm}");
                Console.WriteLine($"當前時間段: {currentTimeOfDay:hh\\:mm}");

                bool isWithinHours = currentTimeOfDay >= equipment.OpenTime &&
                                   currentTimeOfDay <= equipment.CloseTime;

                Console.WriteLine($"是否在開放時間內: {isWithinHours}");

                return isWithinHours;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"檢查開放時間錯誤: {ex.Message}");
                return false;
            }
        }

        // 添加時區轉換方法
        private DateTime GetTaiwanTime()
        {
            // 方法1：使用 TimeZoneInfo（推薦）
            try
            {
                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            }
            catch
            {
                // 方法2：如果找不到時區，使用 UTC+8
                return DateTime.UtcNow.AddHours(8);
            }
        }

        // 通知設備狀態改變
        private void NotifyEquipmentStateChanged(byte equipmentId)
        {
            // 這裡應該從全局的觀察者管理器中獲取對應的Subject並通知
            // 簡化實現，實際應用中可能需要一個單例來管理所有設備的Subject
            var subject = ReservationObserverManager.GetInstance().GetSubject(equipmentId);
            if (subject != null)
            {
                subject.CurrentUsers = GetCurrentUsers(equipmentId);
                subject.WaitingQueue = GetWaitingQueue(equipmentId);
                subject.StateChanged();
            }
        }

        public Equipment GetEquipmentById(byte id)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var cmd = new SqlCommand("SELECT * FROM Equipment WHERE Id = @Id", connection);
                cmd.Parameters.AddWithValue("@Id", id);

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Equipment
                    {
                        Id = reader.GetByte(reader.GetOrdinal("Id")),
                        equipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                        MaxUsers = reader.GetByte(reader.GetOrdinal("MaxUsers")),
                        AvailableTime = reader.GetInt16(reader.GetOrdinal("AvailableTime")),
                        OpenTime = reader.GetTimeSpan(reader.GetOrdinal("OpenTime")),
                        CloseTime = reader.GetTimeSpan(reader.GetOrdinal("CloseTime"))
                    };
                }
            }
            return null;
        }

        // 保留原有的 InsertReservation 方法（無連接參數）
        private void InsertReservation(Reservation reservation)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();
                InsertReservation(reservation, connection); // 調用帶連接參數的版本
            }
        }

        // 添加帶連接參數的 InsertReservation 方法重載
        private void InsertReservation(Reservation reservation, SqlConnection connection)
        {
            // 確保使用北京時間
            //if (reservation.StartTime.Kind != DateTimeKind.Utc)
            //{
            //    reservation.StartTime = GetTaiwanTime();
            //}
            //if (reservation.ReservationTime.Kind != DateTimeKind.Utc)
            //{
            //    reservation.ReservationTime = GetTaiwanTime();
            //}

            var cmd = new SqlCommand(
                "INSERT INTO Reservations (EquipmentId, UserId, StartTime, ReservationTime, Status) VALUES (@EquipmentId, @UserId, @StartTime, @ReservationTime, @Status)",
                connection);
            cmd.Parameters.AddWithValue("@EquipmentId", reservation.EquipmentId);
            cmd.Parameters.AddWithValue("@UserId", reservation.UserId);
            cmd.Parameters.AddWithValue("@StartTime", reservation.StartTime);
            cmd.Parameters.AddWithValue("@ReservationTime", reservation.ReservationTime);
            cmd.Parameters.AddWithValue("@Status", (int)reservation.Status);

            cmd.ExecuteNonQuery();
        }

        private Reservation GetReservationById(int id)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var cmd = new SqlCommand("SELECT * FROM Reservations WHERE Id = @Id", connection);
                cmd.Parameters.AddWithValue("@Id", id);

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Reservation
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                        StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                        EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("EndTime")),
                        ReservationTime = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                        Status = (ReservationStatus)reader.GetInt32(reader.GetOrdinal("Status"))
                    };
                }
            }
            return null;
        }
        // 獲取用戶進行中的預約
        public List<Dictionary<string, object>> GetActiveReservations(string userId)
        {
            // 先處理過期預約
            AutoCompleteExpiredReservations();

            var activeReservations = new List<Dictionary<string, object>>();

            using (var connection = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(@"
                SELECT r.*, e.equipmentName, e.AvailableTime 
                FROM Reservations r
                INNER JOIN Equipment e ON r.EquipmentId = e.Id
                WHERE r.UserId = @UserId AND r.Status = @Status
                ORDER BY r.StartTime DESC", connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var reservation = new Dictionary<string, object>
                        {
                            ["Id"] = reader.GetInt32(reader.GetOrdinal("Id")),
                            ["EquipmentId"] = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                            ["EquipmentName"] = reader.GetString(reader.GetOrdinal("equipmentName")),
                            ["UserId"] = reader.GetString(reader.GetOrdinal("UserId")),
                            ["StartTime"] = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                            ["AvailableTime"] = reader.GetInt16(reader.GetOrdinal("AvailableTime")),
                            ["ReservationTime"] = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                            ["Status"] = reader.GetInt32(reader.GetOrdinal("Status"))
                        };
                        activeReservations.Add(reservation);
                    }
                }
            }

            return activeReservations;
        }


        // 獲取用戶排隊中的記錄
        public List<Dictionary<string, object>> GetWaitingQueues(string userId)
        {
            var waitingQueues = new List<Dictionary<string, object>>();

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT wq.*, e.equipmentName, e.AvailableTime as AverageUsageTime
            FROM WaitingQueue wq
            INNER JOIN Equipment e ON wq.EquipmentId = e.Id
            WHERE wq.UserId = @UserId
            ORDER BY wq.Position",
                    connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var queue = new Dictionary<string, object>
                    {
                        ["Id"] = reader.GetInt32(reader.GetOrdinal("Id")),
                        ["EquipmentId"] = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                        ["EquipmentName"] = reader.GetString(reader.GetOrdinal("equipmentName")),
                        ["UserId"] = reader.GetString(reader.GetOrdinal("UserId")),
                        ["QueueTime"] = reader.GetDateTime(reader.GetOrdinal("QueueTime")),
                        ["Position"] = reader.GetInt32(reader.GetOrdinal("Position")),
                        ["AverageUsageTime"] = reader.GetInt16(reader.GetOrdinal("AverageUsageTime"))
                    };
                    waitingQueues.Add(queue);
                }
            }

            return waitingQueues;
        }

        // 獲取用戶歷史記錄
        public List<Dictionary<string, object>> GetHistoryReservations(string userId)
        {
            var historyReservations = new List<Dictionary<string, object>>();

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var cmd = new SqlCommand(@"
            SELECT r.*, e.equipmentName
            FROM Reservations r
            INNER JOIN Equipment e ON r.EquipmentId = e.Id
            WHERE r.UserId = @UserId AND r.Status IN (@CompletedStatus, @CancelledStatus)
            ORDER BY r.ReservationTime DESC",
                    connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CompletedStatus", (int)ReservationStatus.Completed);
                cmd.Parameters.AddWithValue("@CancelledStatus", (int)ReservationStatus.Cancelled);

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var reservation = new Dictionary<string, object>
                    {
                        ["Id"] = reader.GetInt32(reader.GetOrdinal("Id")),
                        ["EquipmentId"] = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                        ["EquipmentName"] = reader.GetString(reader.GetOrdinal("equipmentName")),
                        ["UserId"] = reader.GetString(reader.GetOrdinal("UserId")),
                        ["StartTime"] = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? null : (object)reader.GetDateTime(reader.GetOrdinal("StartTime")),
                        ["EndTime"] = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? null : (object)reader.GetDateTime(reader.GetOrdinal("EndTime")),
                        ["ReservationTime"] = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                        ["Status"] = reader.GetInt32(reader.GetOrdinal("Status"))
                    };
                    historyReservations.Add(reservation);
                }
            }

            return historyReservations;
        }

        // 取消排隊
        public bool CancelWaitingQueue(int queueId, string userId)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                // 檢查排隊記錄是否存在且屬於該用戶
                var checkCmd = new SqlCommand(
                    "SELECT EquipmentId FROM WaitingQueue WHERE Id = @Id AND UserId = @UserId",
                    connection);
                checkCmd.Parameters.AddWithValue("@Id", queueId);
                checkCmd.Parameters.AddWithValue("@UserId", userId);

                var result = checkCmd.ExecuteScalar();
                if (result == null) return false;

                byte equipmentId = (byte)result;

                // 刪除排隊記錄
                var deleteCmd = new SqlCommand(
                    "DELETE FROM WaitingQueue WHERE Id = @Id AND UserId = @UserId",
                    connection);
                deleteCmd.Parameters.AddWithValue("@Id", queueId);
                deleteCmd.Parameters.AddWithValue("@UserId", userId);

                int rowsAffected = deleteCmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    // 重新計算排隊位置 - 使用帶連接參數的版本
                    RecalculateQueuePositions(equipmentId, connection);

                    // 處理排隊隊列
                    ProcessWaitingQueue(equipmentId);

                    // 通知狀態改變
                    NotifyEquipmentStateChanged(equipmentId);
                }

                return rowsAffected > 0;
            }
        }

        // 保留原有的 RecalculateQueuePositions 方法（無連接參數）
        private void RecalculateQueuePositions(byte equipmentId)
        {
            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();
                RecalculateQueuePositions(equipmentId, connection); // 調用帶連接參數的版本
            }
        }

        // 添加帶連接參數的 RecalculateQueuePositions 方法重載
        private void RecalculateQueuePositions(byte equipmentId, SqlConnection connection)
        {
            // 獲取該設備的所有排隊記錄，按加入時間排序
            var selectCmd = new SqlCommand(
                "SELECT Id FROM WaitingQueue WHERE EquipmentId = @EquipmentId ORDER BY QueueTime",
                connection);
            selectCmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

            var reader = selectCmd.ExecuteReader();
            var queueIds = new List<int>();

            while (reader.Read())
            {
                queueIds.Add(reader.GetInt32(0));
            }
            reader.Close();

            // 更新排隊位置
            for (int i = 0; i < queueIds.Count; i++)
            {
                var updateCmd = new SqlCommand(
                    "UPDATE WaitingQueue SET Position = @Position WHERE Id = @Id",
                    connection);
                updateCmd.Parameters.AddWithValue("@Position", i + 1);
                updateCmd.Parameters.AddWithValue("@Id", queueIds[i]);
                updateCmd.ExecuteNonQuery();
            }
        }
    }
}
