using Microsoft.Data.SqlClient;
using sql.Models;
using sql.Services;

namespace sql.Repositories
{
    // Repository 層的工作很單純：專心跟資料庫溝通。
    // 這樣 Service 不需要知道 SQL 長什麼樣子，分工會更清楚。
    public class ReservationRepository
    {
        private readonly DBmanager _dbManager;
        private readonly EquipmentStateNotifier _equipmentStateNotifier;
        private readonly QueueProcessingCoordinator _queueProcessingCoordinator;

        public ReservationRepository(
            DBmanager dbManager,
            EquipmentStateNotifier equipmentStateNotifier,
            QueueProcessingCoordinator queueProcessingCoordinator)
        {
            _dbManager = dbManager;
            _equipmentStateNotifier = equipmentStateNotifier;
            _queueProcessingCoordinator = queueProcessingCoordinator;
        }

        // 這一輪把建立預約的主要判斷搬進 Repository。
        // 新手可以把它理解成：
        // 1. 先確認設備與使用者狀態
        // 2. 再決定是直接開始使用，還是加入排隊
        public ReservationResult CreateReservation(byte equipmentId, string userKey)
        {
            try
            {
                var equipment = GetEquipmentById(equipmentId);
                if (equipment == null)
                {
                    return new ReservationResult { Success = false, Message = "設備不存在" };
                }

                var taiwanTime = RepositorySqlHelper.GetTaiwanTime();

                // 先擋掉重複預約，避免同一個人反覆建立相同設備的使用中資料。
                if (HasActiveReservationForSameEquipment(equipmentId, userKey))
                {
                    return new ReservationResult
                    {
                        Success = false,
                        Message = "您在此設備已有進行中的預約，無法重複預約"
                    };
                }

                // 如果已經在排隊，就不應該再加一次相同設備的排隊資料。
                if (IsUserInWaitingQueue(equipmentId, userKey))
                {
                    return new ReservationResult
                    {
                        Success = false,
                        Message = "您已在此設備的排隊隊伍中，請耐心等候"
                    };
                }

                if (!IsWithinOperatingHours(equipment, taiwanTime))
                {
                    return new ReservationResult { Success = false, Message = "不在設備開放時間內" };
                }

                var currentUsers = GetCurrentUsers(equipmentId);
                if (currentUsers < equipment.MaxUsers)
                {
                    // 還有名額時，直接建立使用中的預約。
                    var reservation = new Reservation
                    {
                        EquipmentId = equipmentId,
                        UserId = userKey,
                        StartTime = taiwanTime,
                        ReservationTime = taiwanTime,
                        Status = ReservationStatus.InProgress
                    };

                    InsertReservation(reservation);

                    return new ReservationResult
                    {
                        Success = true,
                        Message = "預約成功，立即開始使用"
                    };
                }

                // 沒有名額時，就改成加入排隊並回傳預估等待資訊。
                var queuePosition = AddToWaitingQueue(equipmentId, userKey);
                var estimatedWaitTime = CalculateEstimatedWaitTime(equipment, queuePosition);

                return new ReservationResult
                {
                    Success = true,
                    Message = "設備已滿，已加入排隊",
                    WaitingPosition = queuePosition,
                    EstimatedWaitTime = estimatedWaitTime,
                    ExpectedStartTime = taiwanTime.AddMinutes(estimatedWaitTime)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"建立預約時發生錯誤: {ex.Message}");
                return new ReservationResult { Success = false, Message = "系統錯誤，請稍後再試" };
            }
        }

        // 第二階段真正開始落地「未來時段預約」。
        // 這裡會建立一筆 Scheduled 預約，而不是立即開始使用。
        public ReservationResult CreateScheduledReservation(FutureReservationRequestViewModel request, string userKey)
        {
            try
            {
                var equipment = GetEquipmentById(request.EquipmentId);
                if (equipment == null)
                {
                    return new ReservationResult { Success = false, Message = "設備不存在" };
                }

                if (!DateOnly.TryParse(request.ReservationDate, out var reservationDate))
                {
                    return new ReservationResult { Success = false, Message = "預約日期格式不正確" };
                }

                if (!TimeOnly.TryParse(request.SelectedSlotStartTime, out var slotStartTime))
                {
                    return new ReservationResult { Success = false, Message = "預約時段格式不正確" };
                }

                var taiwanTime = RepositorySqlHelper.GetTaiwanTime();
                var reservedStartTime = reservationDate.ToDateTime(slotStartTime);
                var reservedEndTime = reservedStartTime.AddMinutes(equipment.AvailableTime);
                var latestStartTime = reservationDate.ToDateTime(TimeOnly.MinValue).Add(equipment.CloseTime).AddMinutes(-equipment.AvailableTime);

                if (reservedStartTime <= taiwanTime)
                {
                    return new ReservationResult { Success = false, Message = "未來預約時間必須晚於目前時間" };
                }

                if (reservedStartTime.TimeOfDay < equipment.OpenTime || reservedStartTime > latestStartTime)
                {
                    return new ReservationResult { Success = false, Message = "選擇的時段不在設備可預約範圍內" };
                }

                if (HasAnyInProgressReservation(userKey))
                {
                    return new ReservationResult
                    {
                        Success = false,
                        Message = "您目前已有進行中的預約，請先結束使用後再建立未來預約"
                    };
                }

                if (HasScheduledReservationConflict(userKey, reservedStartTime, reservedEndTime))
                {
                    return new ReservationResult
                    {
                        Success = false,
                        Message = "您在這個時段已有其他預約，請改選其他時間"
                    };
                }

                if (GetScheduledReservationCount(request.EquipmentId, reservedStartTime, reservedEndTime) >= equipment.MaxUsers)
                {
                    return new ReservationResult
                    {
                        Success = false,
                        Message = "此時段的預約名額已滿，請改選其他時間"
                    };
                }

                using var connection = _dbManager.CreateConnection();
                connection.Open();

                using var insertCmd = new SqlCommand(@"
                    INSERT INTO Reservations
                    (
                        EquipmentId,
                        UserId,
                        StartTime,
                        EndTime,
                        ReservationTime,
                        Status,
                        CreatedAt,
                        ReservedStartTime,
                        ReservedEndTime,
                        DurationMinutes,
                        ReservationType
                    )
                    VALUES
                    (
                        @EquipmentId,
                        @UserId,
                        @StartTime,
                        @EndTime,
                        @ReservationTime,
                        @Status,
                        @CreatedAt,
                        @ReservedStartTime,
                        @ReservedEndTime,
                        @DurationMinutes,
                        @ReservationType
                    )", connection);

                insertCmd.Parameters.AddWithValue("@EquipmentId", request.EquipmentId);
                insertCmd.Parameters.AddWithValue("@UserId", userKey);
                insertCmd.Parameters.AddWithValue("@StartTime", reservedStartTime);
                insertCmd.Parameters.AddWithValue("@EndTime", DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ReservationTime", taiwanTime);
                insertCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Scheduled);
                insertCmd.Parameters.AddWithValue("@CreatedAt", taiwanTime);
                insertCmd.Parameters.AddWithValue("@ReservedStartTime", reservedStartTime);
                insertCmd.Parameters.AddWithValue("@ReservedEndTime", reservedEndTime);
                insertCmd.Parameters.AddWithValue("@DurationMinutes", equipment.AvailableTime);
                insertCmd.Parameters.AddWithValue("@ReservationType", 2);
                insertCmd.ExecuteNonQuery();

                return new ReservationResult
                {
                    Success = true,
                    Message = "未來時段預約成功",
                    ScheduledStartTime = reservedStartTime,
                    ScheduledEndTime = reservedEndTime
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"建立未來時段預約時發生錯誤: {ex.Message}");
                return new ReservationResult { Success = false, Message = "建立未來預約失敗，請稍後再試" };
            }
        }

        // 取消預約這一輪已經正式搬到 Repository。
        // 流程是：
        // 1. 先確認這筆預約存在且屬於該使用者
        // 2. 更新狀態為 Cancelled
        // 3. 如果原本是使用中，再通知設備狀態變化
        public bool CancelReservation(int reservationId, string userKey)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            using var checkCmd = new SqlCommand(
                "SELECT Status FROM Reservations WHERE Id = @Id AND UserId = @UserId",
                connection);
            checkCmd.Parameters.AddWithValue("@Id", reservationId);
            checkCmd.Parameters.AddWithValue("@UserId", userKey);

            var statusValue = checkCmd.ExecuteScalar();
            if (statusValue == null)
            {
                return false;
            }

            var currentStatus = (ReservationStatus)Convert.ToInt32(statusValue);

            using var updateCmd = new SqlCommand(
                "UPDATE Reservations SET Status = @Status, EndTime = @EndTime WHERE Id = @Id",
                connection);
            updateCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Cancelled);
            updateCmd.Parameters.AddWithValue("@EndTime", DateTime.Now);
            updateCmd.Parameters.AddWithValue("@Id", reservationId);

            var rowsAffected = updateCmd.ExecuteNonQuery();
            if (rowsAffected <= 0)
            {
                return false;
            }

            // 為了維持既有行為，如果取消的是使用中預約，
            // 仍透過舊流程觸發設備狀態更新。
            if (currentStatus == ReservationStatus.InProgress)
            {
                var reservation = GetReservationById(reservationId);
                if (reservation != null)
                {
                    _queueProcessingCoordinator.ProcessEquipmentQueue(reservation.EquipmentId);
                }
            }

            return true;
        }

        // 結束使用的流程比取消預約多一步：
        // 成功更新成 Completed 後，要立即處理排隊遞補。
        public bool EndUsage(int reservationId, string userKey)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            using var checkCmd = new SqlCommand(
                "SELECT EquipmentId FROM Reservations WHERE Id = @Id AND UserId = @UserId AND Status = @Status",
                connection);
            checkCmd.Parameters.AddWithValue("@Id", reservationId);
            checkCmd.Parameters.AddWithValue("@UserId", userKey);
            checkCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

            var result = checkCmd.ExecuteScalar();
            if (result == null)
            {
                return false;
            }

            var equipmentId = (byte)result;

            using var updateCmd = new SqlCommand(
                "UPDATE Reservations SET Status = @Status, EndTime = @EndTime WHERE Id = @Id",
                connection);
            updateCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Completed);
            updateCmd.Parameters.AddWithValue("@EndTime", DateTime.Now);
            updateCmd.Parameters.AddWithValue("@Id", reservationId);

            var rowsAffected = updateCmd.ExecuteNonQuery();
            if (rowsAffected <= 0)
            {
                return false;
            }

            _queueProcessingCoordinator.ProcessEquipmentQueue(equipmentId);
            return true;
        }

        public void AutoCompleteExpiredReservations()
        {
            try
            {
                using var connection = _dbManager.CreateConnection();
                connection.Open();

                // 這個流程的目的很明確：
                // 找出所有已超時但狀態還停在使用中的預約，批次改成 Completed。
                var expiredReservations = GetExpiredReservations(connection);
                foreach (var reservation in expiredReservations)
                {
                    try
                    {
                        using var updateCmd = new SqlCommand(@"
                            UPDATE Reservations
                            SET Status = @CompletedStatus, EndTime = @EndTime
                            WHERE Id = @Id", connection);
                        updateCmd.Parameters.AddWithValue("@CompletedStatus", (int)ReservationStatus.Completed);
                        updateCmd.Parameters.AddWithValue("@EndTime", RepositorySqlHelper.GetTaiwanTime());
                        updateCmd.Parameters.AddWithValue("@Id", reservation.Id);
                        updateCmd.ExecuteNonQuery();

                        // 預約結束後，要立刻嘗試推進排隊，避免設備空著。
                        _queueProcessingCoordinator.ProcessEquipmentQueue(reservation.EquipmentId);
                        _equipmentStateNotifier.Notify(reservation.EquipmentId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"更新過期預約時錯誤 (ID: {reservation.Id}): {ex.Message}");
                    }
                }

                // 過期預約處理完後，再檢查是否有已到預約時間的 Scheduled 預約。
                // 這樣背景服務每次跑時，就能一起推進未來預約的狀態流轉。
                ProcessDueScheduledReservations();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AutoCompleteExpiredReservations 整體錯誤: {ex.Message}");
            }
        }

        // 這是重構後第一批真正搬出來的 SQL 查詢。
        // 讀取類查詢通常比寫入安全，很適合拿來當重構起手式。
        // 這一輪再把 Dictionary 收斂成明確 DTO，讓欄位意義更清楚。
        public List<ActiveReservationItem> GetActiveReservations(string userKey)
        {
            // 先清掉過期資料，再查目前使用中的資料，避免畫面顯示舊狀態。
            AutoCompleteExpiredReservations();

            var activeReservations = new List<ActiveReservationItem>();

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT r.*, e.equipmentName, e.AvailableTime
                FROM Reservations r
                INNER JOIN Equipment e ON r.EquipmentId = e.Id
                WHERE r.UserId = @UserId AND r.Status = @Status
                ORDER BY r.StartTime DESC", connection);

            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                activeReservations.Add(new ActiveReservationItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                    EquipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                    AvailableTime = reader.GetInt16(reader.GetOrdinal("AvailableTime")),
                    ReservationTime = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                    Status = reader.GetInt32(reader.GetOrdinal("Status"))
                });
            }

            return activeReservations;
        }

        public List<ScheduledReservationItem> GetScheduledReservations(string userKey)
        {
            var scheduledReservations = new List<ScheduledReservationItem>();

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT r.Id,
                       r.EquipmentId,
                       e.equipmentName,
                       r.UserId,
                       r.ReservationTime,
                       r.ReservedStartTime,
                       r.ReservedEndTime,
                       r.DurationMinutes,
                       r.Status
                FROM Reservations r
                INNER JOIN Equipment e ON r.EquipmentId = e.Id
                WHERE r.UserId = @UserId
                  AND r.Status = @Status
                ORDER BY r.ReservedStartTime ASC", connection);

            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Scheduled);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                scheduledReservations.Add(new ScheduledReservationItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                    EquipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    ReservationTime = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                    ReservedStartTime = reader.GetDateTime(reader.GetOrdinal("ReservedStartTime")),
                    ReservedEndTime = reader.GetDateTime(reader.GetOrdinal("ReservedEndTime")),
                    DurationMinutes = reader.IsDBNull(reader.GetOrdinal("DurationMinutes"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                    Status = reader.GetInt32(reader.GetOrdinal("Status"))
                });
            }

            return scheduledReservations;
        }

        public List<WaitingReservationItem> GetWaitingQueues(string userKey)
        {
            var waitingQueues = new List<WaitingReservationItem>();

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT wq.*, e.equipmentName, e.AvailableTime as AverageUsageTime
                FROM WaitingQueue wq
                INNER JOIN Equipment e ON wq.EquipmentId = e.Id
                WHERE wq.UserId = @UserId
                ORDER BY COALESCE(wq.QueuePosition, wq.Position)", connection);

            cmd.Parameters.AddWithValue("@UserId", userKey);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                waitingQueues.Add(new WaitingReservationItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                    EquipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    QueueTime = reader.GetDateTime(reader.GetOrdinal("QueueTime")),
                    Position = reader.GetInt32(reader.GetOrdinal("Position")),
                    AverageUsageTime = reader.GetInt16(reader.GetOrdinal("AverageUsageTime")),
                    QueueType = reader.IsDBNull(reader.GetOrdinal("QueueType"))
                        ? 1
                        : reader.GetInt32(reader.GetOrdinal("QueueType"))
                });
            }

            return waitingQueues;
        }

        public List<HistoryReservationItem> GetHistoryReservations(string userKey)
        {
            var historyReservations = new List<HistoryReservationItem>();

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT r.*, e.equipmentName
                FROM Reservations r
                INNER JOIN Equipment e ON r.EquipmentId = e.Id
                WHERE r.UserId = @UserId AND r.Status IN (@CompletedStatus, @CancelledStatus)
                ORDER BY r.ReservationTime DESC", connection);

            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@CompletedStatus", (int)ReservationStatus.Completed);
            cmd.Parameters.AddWithValue("@CancelledStatus", (int)ReservationStatus.Cancelled);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                historyReservations.Add(new HistoryReservationItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                    EquipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    StartTime = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? null : reader.GetDateTime(reader.GetOrdinal("StartTime")),
                    EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                    ReservationTime = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                    Status = reader.GetInt32(reader.GetOrdinal("Status"))
                });
            }

            return historyReservations;
        }

        public Equipment? GetEquipmentById(byte equipmentId)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();
            return RepositorySqlHelper.GetEquipmentById(connection, equipmentId);
        }

        public int GetCurrentUsers(byte equipmentId)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            return RepositorySqlHelper.GetCurrentUsers(connection, equipmentId);
        }

        // 這個檢查對應目前舊系統的規則：
        // 先只擋掉同設備、而且仍在使用中的重複預約。
        private bool HasActiveReservationForSameEquipment(byte equipmentId, string userKey)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Reservations
                WHERE EquipmentId = @EquipmentId
                  AND UserId = @UserId
                  AND Status = @Status", connection);

            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

            connection.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // 第二階段規則：若使用者已在其他設備使用中，先不要再建立未來預約。
        private bool HasAnyInProgressReservation(string userKey)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Reservations
                WHERE UserId = @UserId
                  AND Status = @Status", connection);

            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

            connection.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // 同一個人在相同時間區間內，不應該有互相重疊的未來預約。
        private bool HasScheduledReservationConflict(string userKey, DateTime reservedStartTime, DateTime reservedEndTime)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Reservations
                WHERE UserId = @UserId
                  AND Status = @Status
                  AND ReservedStartTime < @ReservedEndTime
                  AND ReservedEndTime > @ReservedStartTime", connection);

            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@ReservedStartTime", reservedStartTime);
            cmd.Parameters.AddWithValue("@ReservedEndTime", reservedEndTime);

            connection.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // 同一個設備在同一個時段最多只能容納 MaxUsers 筆 Scheduled 預約。
        private int GetScheduledReservationCount(byte equipmentId, DateTime reservedStartTime, DateTime reservedEndTime)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Reservations
                WHERE EquipmentId = @EquipmentId
                  AND Status = @Status
                  AND ReservedStartTime < @ReservedEndTime
                  AND ReservedEndTime > @ReservedStartTime", connection);

            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@ReservedStartTime", reservedStartTime);
            cmd.Parameters.AddWithValue("@ReservedEndTime", reservedEndTime);

            connection.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // 這個檢查是為了避免同一個人重複插入相同設備的排隊資料。
        private bool IsUserInWaitingQueue(byte equipmentId, string userKey)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM WaitingQueue
                WHERE EquipmentId = @EquipmentId
                  AND UserId = @UserId", connection);

            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            cmd.Parameters.AddWithValue("@UserId", userKey);

            connection.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // 加入排隊的邏輯目前很適合先獨立成小方法，
        // 之後如果排隊規則改版，只需要來這裡改。
        private int AddToWaitingQueue(byte equipmentId, string userKey)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();
            return AddToWaitingQueue(connection, equipmentId, userKey, null, 1);
        }

        // 這個版本是給第二階段「預約到點轉排隊」使用。
        // 會同時把 ReservationId / QueueType 寫進去，讓之後排到時能回頭更新同一筆預約。
        private int AddToWaitingQueue(SqlConnection connection, byte equipmentId, string userKey, int? reservationId, int queueType)
        {
            var queueTime = RepositorySqlHelper.GetTaiwanTime();

            using var countCmd = new SqlCommand(
                "SELECT COUNT(*) FROM WaitingQueue WHERE EquipmentId = @EquipmentId",
                connection);
            countCmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

            var queueCount = Convert.ToInt32(countCmd.ExecuteScalar());
            var position = queueCount + 1;

            using var insertCmd = new SqlCommand(@"
                INSERT INTO WaitingQueue
                (
                    EquipmentId,
                    UserId,
                    QueueTime,
                    Position,
                    ReservationId,
                    QueueType,
                    QueueStatus,
                    QueuedAt,
                    QueuePosition,
                    ExpectedAvailableTime
                )
                VALUES
                (
                    @EquipmentId,
                    @UserId,
                    @QueueTime,
                    @Position,
                    @ReservationId,
                    @QueueType,
                    @QueueStatus,
                    @QueuedAt,
                    @QueuePosition,
                    @ExpectedAvailableTime
                )",
                connection);
            insertCmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            insertCmd.Parameters.AddWithValue("@UserId", userKey);
            insertCmd.Parameters.AddWithValue("@QueueTime", queueTime);
            insertCmd.Parameters.AddWithValue("@Position", position);
            insertCmd.Parameters.AddWithValue("@ReservationId", reservationId.HasValue ? reservationId.Value : DBNull.Value);
            insertCmd.Parameters.AddWithValue("@QueueType", queueType);
            insertCmd.Parameters.AddWithValue("@QueueStatus", 1);
            insertCmd.Parameters.AddWithValue("@QueuedAt", queueTime);
            insertCmd.Parameters.AddWithValue("@QueuePosition", position);
            insertCmd.Parameters.AddWithValue("@ExpectedAvailableTime", DBNull.Value);
            insertCmd.ExecuteNonQuery();

            return position;
        }

        // 目前維持舊系統的簡化估算方式：
        // 排隊順位 * 設備平均使用時間。
        private int CalculateEstimatedWaitTime(Equipment equipment, int queuePosition)
        {
            return queuePosition * (equipment.AvailableTime / 60);
        }

        // 開放時間檢查先維持原規則，之後若有跨日營業需求再另外擴充。
        private bool IsWithinOperatingHours(Equipment equipment, DateTime currentTime)
        {
            var currentTimeOfDay = currentTime.TimeOfDay;
            return currentTimeOfDay >= equipment.OpenTime && currentTimeOfDay <= equipment.CloseTime;
        }

        // 用小方法包住 INSERT，可以讓主流程更像在讀業務規則。
        private void InsertReservation(Reservation reservation)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();
            InsertReservation(reservation, connection);
        }

        private void InsertReservation(Reservation reservation, SqlConnection connection)
        {
            RepositorySqlHelper.InsertReservation(connection, reservation);
        }

        // 這是 Repository 內部使用的小工具方法，
        // 用來在需要時取回一筆完整的預約資料。
        private Reservation? GetReservationById(int reservationId)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand("SELECT * FROM Reservations WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", reservationId);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new Reservation
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? null : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                ReservationTime = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                Status = (ReservationStatus)reader.GetInt32(reader.GetOrdinal("Status"))
            };
        }

        // 先把過期預約整理成清單，再進行更新，
        // 這樣可以避免一邊讀取資料，一邊修改同一批資料造成流程混亂。
        private List<ExpiredReservationInfo> GetExpiredReservations(SqlConnection connection)
        {
            var expiredReservations = new List<ExpiredReservationInfo>();

            using var selectCmd = new SqlCommand(@"
                SELECT r.Id, r.EquipmentId, r.UserId, e.AvailableTime
                FROM Reservations r
                INNER JOIN Equipment e ON r.EquipmentId = e.Id
                WHERE r.Status = @Status
                  AND DATEADD(MINUTE, e.AvailableTime, r.StartTime) <= @CurrentTime", connection);
            selectCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);
            selectCmd.Parameters.AddWithValue("@CurrentTime", RepositorySqlHelper.GetTaiwanTime());

            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    expiredReservations.Add(new ExpiredReservationInfo
                    {
                        Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? 0 : reader.GetInt32(reader.GetOrdinal("Id")),
                        EquipmentId = reader.IsDBNull(reader.GetOrdinal("EquipmentId")) ? (byte)0 : reader.GetByte(reader.GetOrdinal("EquipmentId")),
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? string.Empty : reader.GetString(reader.GetOrdinal("UserId")),
                        AvailableTime = reader.IsDBNull(reader.GetOrdinal("AvailableTime")) ? (short)0 : reader.GetInt16(reader.GetOrdinal("AvailableTime"))
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"讀取過期預約資料時錯誤: {ex.Message}");
                }
            }

            return expiredReservations;
        }

        private class ExpiredReservationInfo
        {
            public int Id { get; set; }
            public byte EquipmentId { get; set; }
            public string UserId { get; set; } = string.Empty;
            public short AvailableTime { get; set; }
        }

        // 這是第二階段「預約到點後流轉」的核心：
        // 1. 已到預約時間
        // 2. 如果目前沒有人排隊且設備有空位，就直接轉為使用中
        // 3. 否則把這筆預約併入排隊尾端，等後續依順位補上
        private void ProcessDueScheduledReservations()
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            var dueReservations = GetDueScheduledReservations(connection);
            foreach (var reservation in dueReservations)
            {
                var equipment = RepositorySqlHelper.GetEquipmentById(connection, reservation.EquipmentId);
                if (equipment == null)
                {
                    continue;
                }

                var currentUsers = RepositorySqlHelper.GetCurrentUsers(connection, reservation.EquipmentId);
                var waitingCount = GetWaitingQueueCount(connection, reservation.EquipmentId);

                if (currentUsers < equipment.MaxUsers && waitingCount == 0)
                {
                    PromoteScheduledReservationToInProgress(connection, reservation.Id);
                    _equipmentStateNotifier.Notify(reservation.EquipmentId);
                    continue;
                }

                if (IsReservationAlreadyInQueue(connection, reservation.Id))
                {
                    continue;
                }

                AddToWaitingQueue(connection, reservation.EquipmentId, reservation.UserId, reservation.Id, 2);
                MarkScheduledReservationAsWaiting(connection, reservation.Id);

                // 併入排隊後立刻嘗試跑一次補位，
                // 這樣若前面隊列其實已可前進，就不用等下一輪背景服務。
                _queueProcessingCoordinator.ProcessEquipmentQueue(reservation.EquipmentId);
            }
        }

        private static List<DueScheduledReservationInfo> GetDueScheduledReservations(SqlConnection connection)
        {
            var reservations = new List<DueScheduledReservationInfo>();

            using var cmd = new SqlCommand(@"
                SELECT Id, EquipmentId, UserId, ReservedStartTime
                FROM Reservations
                WHERE Status = @Status
                  AND ReservedStartTime IS NOT NULL
                  AND ReservedStartTime <= @CurrentTime
                ORDER BY ReservedStartTime, Id", connection);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@CurrentTime", RepositorySqlHelper.GetTaiwanTime());

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reservations.Add(new DueScheduledReservationInfo
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    ReservedStartTime = reader.GetDateTime(reader.GetOrdinal("ReservedStartTime"))
                });
            }

            return reservations;
        }

        private static int GetWaitingQueueCount(SqlConnection connection, byte equipmentId)
        {
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM WaitingQueue WHERE EquipmentId = @EquipmentId",
                connection);
            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static bool IsReservationAlreadyInQueue(SqlConnection connection, int reservationId)
        {
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM WaitingQueue WHERE ReservationId = @ReservationId",
                connection);
            cmd.Parameters.AddWithValue("@ReservationId", reservationId);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void PromoteScheduledReservationToInProgress(SqlConnection connection, int reservationId)
        {
            var taiwanTime = RepositorySqlHelper.GetTaiwanTime();

            using var cmd = new SqlCommand(@"
                UPDATE Reservations
                SET Status = @Status,
                    StartTime = @StartTime,
                    ActualStartTime = @ActualStartTime
                WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);
            cmd.Parameters.AddWithValue("@StartTime", taiwanTime);
            cmd.Parameters.AddWithValue("@ActualStartTime", taiwanTime);
            cmd.Parameters.AddWithValue("@Id", reservationId);
            cmd.ExecuteNonQuery();
        }

        private static void MarkScheduledReservationAsWaiting(SqlConnection connection, int reservationId)
        {
            using var cmd = new SqlCommand(@"
                UPDATE Reservations
                SET Status = @Status
                WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Waiting);
            cmd.Parameters.AddWithValue("@Id", reservationId);
            cmd.ExecuteNonQuery();
        }

        private class DueScheduledReservationInfo
        {
            public int Id { get; set; }
            public byte EquipmentId { get; set; }
            public string UserId { get; set; } = string.Empty;
            public DateTime ReservedStartTime { get; set; }
        }
    }
}
