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
                var futureReservedCapacityCount = GetReservedCapacityCount(
                    equipmentId,
                    taiwanTime,
                    taiwanTime.AddMinutes(equipment.AvailableTime));

                // 立即預約不能只看「現在還有沒有空位」。
                // 如果這次立即使用會侵占後面已經被未來預約保留的容量，
                // 就必須改走排隊，而不是直接開始使用。
                var effectiveCurrentCapacity = Math.Max(0, equipment.MaxUsers - futureReservedCapacityCount);

                if (currentUsers < effectiveCurrentCapacity)
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
                // 這裡包含兩種情況：
                // 1. 設備當下已滿
                // 2. 雖然當下有空位，但後面已有未來預約先保留了容量
                var queuePosition = AddToWaitingQueue(equipmentId, userKey);
                var estimatedWaitTime = CalculateEstimatedWaitTime(equipment, queuePosition);

                return new ReservationResult
                {
                    Success = true,
                    Message = futureReservedCapacityCount > 0 && currentUsers < equipment.MaxUsers
                        ? "此設備後續時段已有未來預約保留名額，已改為加入排隊"
                        : "設備已滿，已加入排隊",
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
                    return new ReservationResult { Success = false, Message = "該時段已過期，請刷新頁面" };
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

                var forecast = BuildFutureReservationForecast(equipment, reservedStartTime, reservedEndTime);
                if (forecast.HasReservedCapacityConflict)
                {
                    return new ReservationResult
                    {
                        Success = false,
                        Message = forecast.Message
                    };
                }

                if (forecast.QueueExpected && !request.ConfirmQueueExpected)
                {
                    return new ReservationResult
                    {
                        Success = false,
                        RequiresConfirmation = true,
                        QueueExpected = true,
                        Message = forecast.Message,
                        ScheduledStartTime = reservedStartTime,
                        ScheduledEndTime = reservedEndTime
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
                insertCmd.Parameters.AddWithValue(
                    "@Status",
                    (int)(forecast.QueueExpected
                        ? ReservationStatus.ScheduledQueueExpected
                        : ReservationStatus.Scheduled));
                insertCmd.Parameters.AddWithValue("@CreatedAt", taiwanTime);
                insertCmd.Parameters.AddWithValue("@ReservedStartTime", reservedStartTime);
                insertCmd.Parameters.AddWithValue("@ReservedEndTime", reservedEndTime);
                insertCmd.Parameters.AddWithValue("@DurationMinutes", equipment.AvailableTime);
                insertCmd.Parameters.AddWithValue("@ReservationType", forecast.QueueExpected ? 3 : 2);
                insertCmd.ExecuteNonQuery();

                return new ReservationResult
                {
                    Success = true,
                    Message = forecast.QueueExpected
                        ? "未來時段預約已建立，但依目前推算到時仍可能需要排隊，系統會到點時自動併入排隊尾端"
                        : "未來時段預約成功",
                    QueueExpected = forecast.QueueExpected,
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

        // 管理者強制結束使用時，不需要比對預約擁有者，
        // 但仍然只允許針對「使用中」的資料執行，避免誤傷其他狀態。
        public bool ForceEndUsage(int reservationId, int? managerUserId)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            using var checkCmd = new SqlCommand(
                "SELECT EquipmentId FROM Reservations WHERE Id = @Id AND Status = @Status",
                connection);
            checkCmd.Parameters.AddWithValue("@Id", reservationId);
            checkCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

            var result = checkCmd.ExecuteScalar();
            if (result == null)
            {
                return false;
            }

            var equipmentId = (byte)result;

            using var updateCmd = new SqlCommand(@"
                UPDATE Reservations
                SET Status = @Status,
                    EndTime = @EndTime,
                    ActualEndTime = @ActualEndTime,
                    EndedByType = @EndedByType,
                    EndedByUserId = @EndedByUserId
                WHERE Id = @Id", connection);
            updateCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Completed);
            updateCmd.Parameters.AddWithValue("@EndTime", RepositorySqlHelper.GetTaiwanTime());
            updateCmd.Parameters.AddWithValue("@ActualEndTime", RepositorySqlHelper.GetTaiwanTime());
            updateCmd.Parameters.AddWithValue("@EndedByType", 2);
            updateCmd.Parameters.AddWithValue("@EndedByUserId", managerUserId.HasValue ? (object)managerUserId.Value : DBNull.Value);
            updateCmd.Parameters.AddWithValue("@Id", reservationId);

            var rowsAffected = updateCmd.ExecuteNonQuery();
            if (rowsAffected <= 0)
            {
                return false;
            }

            _queueProcessingCoordinator.ProcessEquipmentQueue(equipmentId);
            return true;
        }

        // 管理者可直接取消尚未開始的未來預約。
        // 這裡只允許 Scheduled / ScheduledQueueExpected，
        // 避免把已在使用中或已完成的資料誤取消。
        public bool ForceCancelScheduledReservation(int reservationId, int? managerUserId)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            using var checkCmd = new SqlCommand(@"
                SELECT EquipmentId
                FROM Reservations
                WHERE Id = @Id
                  AND Status IN (@ScheduledStatus, @ScheduledQueueExpectedStatus)", connection);
            checkCmd.Parameters.AddWithValue("@Id", reservationId);
            checkCmd.Parameters.AddWithValue("@ScheduledStatus", (int)ReservationStatus.Scheduled);
            checkCmd.Parameters.AddWithValue("@ScheduledQueueExpectedStatus", (int)ReservationStatus.ScheduledQueueExpected);

            var result = checkCmd.ExecuteScalar();
            if (result == null)
            {
                return false;
            }

            using var updateCmd = new SqlCommand(@"
                UPDATE Reservations
                SET Status = @Status,
                    CancelledAt = @CancelledAt,
                    CancelReason = @CancelReason,
                    CancelledByUserId = @CancelledByUserId
                WHERE Id = @Id", connection);
            updateCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.Cancelled);
            updateCmd.Parameters.AddWithValue("@CancelledAt", RepositorySqlHelper.GetTaiwanTime());
            updateCmd.Parameters.AddWithValue("@CancelReason", "管理者於後台取消未來預約");
            updateCmd.Parameters.AddWithValue("@CancelledByUserId", managerUserId.HasValue ? (object)managerUserId.Value : DBNull.Value);
            updateCmd.Parameters.AddWithValue("@Id", reservationId);

            return updateCmd.ExecuteNonQuery() > 0;
        }

        // 管理者調整未來預約時段時，要重新套用一遍未來預約規則。
        // 這樣可以確保後台調整與前台建立走的是同一套保留名額與排隊推算邏輯。
        public ReservationResult RescheduleScheduledReservation(
            AdminRescheduleReservationFormViewModel request,
            int? managerUserId)
        {
            try
            {
                _ = managerUserId;

                var evaluation = EvaluateRescheduleScheduledReservation(request);
                if (!evaluation.CanProceed || evaluation.ExistingReservation == null)
                {
                    return new ReservationResult
                    {
                        Success = false,
                        Message = evaluation.Message
                    };
                }

                if (evaluation.Forecast!.QueueExpected && !request.ConfirmQueueExpected)
                {
                    return new ReservationResult
                    {
                        Success = false,
                        RequiresConfirmation = true,
                        QueueExpected = true,
                        Message = evaluation.Message,
                        ScheduledStartTime = evaluation.ReservedStartTime,
                        ScheduledEndTime = evaluation.ReservedEndTime
                    };
                }

                using var connection = _dbManager.CreateConnection();
                connection.Open();

                using var updateCmd = new SqlCommand(@"
                    UPDATE Reservations
                    SET StartTime = @StartTime,
                        ReservedStartTime = @ReservedStartTime,
                        ReservedEndTime = @ReservedEndTime,
                        DurationMinutes = @DurationMinutes,
                        Status = @Status,
                        ReservationType = @ReservationType,
                        ActualStartTime = NULL,
                        EndTime = NULL,
                        ActualEndTime = NULL,
                        CancelledAt = NULL,
                        CancelReason = NULL,
                        CancelledByUserId = NULL,
                        EndedByType = NULL,
                        EndedByUserId = NULL
                    WHERE Id = @Id", connection);
                updateCmd.Parameters.AddWithValue("@StartTime", evaluation.ReservedStartTime);
                updateCmd.Parameters.AddWithValue("@ReservedStartTime", evaluation.ReservedStartTime);
                updateCmd.Parameters.AddWithValue("@ReservedEndTime", evaluation.ReservedEndTime);
                updateCmd.Parameters.AddWithValue("@DurationMinutes", evaluation.Equipment!.AvailableTime);
                updateCmd.Parameters.AddWithValue(
                    "@Status",
                    (int)(evaluation.Forecast.QueueExpected
                        ? ReservationStatus.ScheduledQueueExpected
                        : ReservationStatus.Scheduled));
                updateCmd.Parameters.AddWithValue("@ReservationType", evaluation.Forecast.QueueExpected ? 3 : 2);
                updateCmd.Parameters.AddWithValue("@Id", evaluation.ExistingReservation.Id);

                if (updateCmd.ExecuteNonQuery() <= 0)
                {
                    return new ReservationResult
                    {
                        Success = false,
                        Message = "調整預約時段失敗，請稍後再試"
                    };
                }

                return new ReservationResult
                {
                    Success = true,
                    Message = evaluation.Forecast.QueueExpected
                        ? "管理者已調整預約時段，但依目前推算到時仍可能需要排隊"
                        : "管理者已成功調整預約時段",
                    QueueExpected = evaluation.Forecast.QueueExpected,
                    ScheduledStartTime = evaluation.ReservedStartTime,
                    ScheduledEndTime = evaluation.ReservedEndTime
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"管理者調整未來預約時段時發生錯誤: {ex.Message}");
                return new ReservationResult
                {
                    Success = false,
                    Message = "調整預約時段失敗，請稍後再試"
                };
            }
        }

        public ReservationAdjustmentPreviewResponse PreviewRescheduleScheduledReservation(
            AdminRescheduleReservationFormViewModel request)
        {
            try
            {
                var evaluation = EvaluateRescheduleScheduledReservation(request);
                if (!evaluation.CanProceed)
                {
                    return new ReservationAdjustmentPreviewResponse
                    {
                        CanReschedule = false,
                        Message = evaluation.Message
                    };
                }

                return new ReservationAdjustmentPreviewResponse
                {
                    CanReschedule = true,
                    QueueExpected = evaluation.Forecast!.QueueExpected,
                    ReservedCapacityCount = evaluation.Forecast.ReservedCapacityCount,
                    ForecastWaitingCount = evaluation.Forecast.ForecastWaitingCount,
                    TargetStatusText = evaluation.Forecast.QueueExpected
                        ? ReservationDisplayHelper.GetStatusText((int)ReservationStatus.ScheduledQueueExpected)
                        : ReservationDisplayHelper.GetStatusText((int)ReservationStatus.Scheduled),
                    Message = evaluation.Message,
                    ScheduledStartTimeText = evaluation.ReservedStartTime.ToString("yyyy-MM-dd HH:mm"),
                    ScheduledEndTimeText = evaluation.ReservedEndTime.ToString("yyyy-MM-dd HH:mm")
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"預覽調整未來預約影響時發生錯誤: {ex.Message}");
                return new ReservationAdjustmentPreviewResponse
                {
                    CanReschedule = false,
                    Message = "無法預覽這次調整的影響，請稍後再試"
                };
            }
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

                // 過期預約處理完後，再檢查是否有已到預約時間的 Scheduled / ScheduledQueueExpected 預約。
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
                  AND r.Status IN (@ScheduledStatus, @ScheduledQueueExpectedStatus)
                ORDER BY r.ReservedStartTime ASC", connection);

            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@ScheduledStatus", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@ScheduledQueueExpectedStatus", (int)ReservationStatus.ScheduledQueueExpected);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                scheduledReservations.Add(CreateScheduledReservationItem(reader));
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
                        : reader.GetInt32(reader.GetOrdinal("QueueType")),
                    QueueTypeText = ReservationDisplayHelper.GetQueueTypeText(
                        reader.IsDBNull(reader.GetOrdinal("QueueType"))
                            ? 1
                            : reader.GetInt32(reader.GetOrdinal("QueueType")))
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
                    Status = reader.GetInt32(reader.GetOrdinal("Status")),
                    StatusText = ReservationDisplayHelper.GetStatusText(reader.GetInt32(reader.GetOrdinal("Status"))),
                    StatusCssClass = ReservationDisplayHelper.GetStatusCssClass(reader.GetInt32(reader.GetOrdinal("Status")))
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

        public List<ScheduledReservationItem> GetAllScheduledReservations()
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
                WHERE r.Status IN (@ScheduledStatus, @ScheduledQueueExpectedStatus)
                ORDER BY r.ReservedStartTime, r.Id", connection);

            cmd.Parameters.AddWithValue("@ScheduledStatus", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@ScheduledQueueExpectedStatus", (int)ReservationStatus.ScheduledQueueExpected);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                scheduledReservations.Add(CreateScheduledReservationItem(reader));
            }

            return scheduledReservations;
        }

        public List<ActiveReservationItem> GetAllActiveReservations()
        {
            var activeReservations = new List<ActiveReservationItem>();

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT r.*, e.equipmentName, e.AvailableTime
                FROM Reservations r
                INNER JOIN Equipment e ON r.EquipmentId = e.Id
                WHERE r.Status = @Status
                ORDER BY r.StartTime DESC", connection);

            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var status = reader.GetInt32(reader.GetOrdinal("Status"));
                activeReservations.Add(new ActiveReservationItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                    EquipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                    AvailableTime = reader.GetInt16(reader.GetOrdinal("AvailableTime")),
                    ReservationTime = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                    Status = status,
                    StatusText = ReservationDisplayHelper.GetStatusText(status),
                    StatusCssClass = ReservationDisplayHelper.GetStatusCssClass(status)
                });
            }

            return activeReservations;
        }

        public List<WaitingReservationItem> GetAllWaitingReservations()
        {
            var waitingReservations = new List<WaitingReservationItem>();

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT wq.*, e.equipmentName, e.AvailableTime as AverageUsageTime
                FROM WaitingQueue wq
                INNER JOIN Equipment e ON wq.EquipmentId = e.Id
                ORDER BY wq.EquipmentId, COALESCE(wq.QueuePosition, wq.Position)", connection);

            connection.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var queueType = reader.IsDBNull(reader.GetOrdinal("QueueType"))
                    ? 1
                    : reader.GetInt32(reader.GetOrdinal("QueueType"));

                waitingReservations.Add(new WaitingReservationItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId")),
                    EquipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                    UserId = reader.GetString(reader.GetOrdinal("UserId")),
                    QueueTime = reader.GetDateTime(reader.GetOrdinal("QueueTime")),
                    Position = reader.GetInt32(reader.GetOrdinal("Position")),
                    AverageUsageTime = reader.GetInt16(reader.GetOrdinal("AverageUsageTime")),
                    QueueType = queueType,
                    QueueTypeText = ReservationDisplayHelper.GetQueueTypeText(queueType)
                });
            }

            return waitingReservations;
        }

        public EquipmentReservationChainResponse GetEquipmentReservationChain(byte equipmentId)
        {
            var equipment = GetEquipmentById(equipmentId);
            if (equipment == null)
            {
                return new EquipmentReservationChainResponse
                {
                    EquipmentId = equipmentId
                };
            }

            return new EquipmentReservationChainResponse
            {
                EquipmentId = equipmentId,
                EquipmentName = equipment.equipmentName,
                ScheduledReservations = GetAllScheduledReservations()
                    .Where(r => r.EquipmentId == equipmentId)
                    .OrderBy(r => r.ReservedStartTime)
                    .ToList(),
                ActiveReservations = GetAllActiveReservations()
                    .Where(r => r.EquipmentId == equipmentId)
                    .OrderBy(r => r.StartTime)
                    .ToList(),
                WaitingReservations = GetAllWaitingReservations()
                    .Where(r => r.EquipmentId == equipmentId)
                    .OrderBy(r => r.Position)
                    .ToList()
            };
        }

        // 這個方法會回傳某個時段已被未來預約保留掉的名額數。
        // 後面立即預約與未來預約都會用到它，避免不同流程各算一套。
        public int GetReservedCapacityCount(
            byte equipmentId,
            DateTime reservedStartTime,
            DateTime reservedEndTime,
            int? excludeReservationId = null)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Reservations
                WHERE EquipmentId = @EquipmentId
                  AND Status IN (@ScheduledStatus, @ScheduledQueueExpectedStatus)
                  AND (@ExcludeReservationId IS NULL OR Id <> @ExcludeReservationId)
                  AND ReservedStartTime < @ReservedEndTime
                  AND ReservedEndTime > @ReservedStartTime", connection);

            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            cmd.Parameters.AddWithValue("@ScheduledStatus", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@ScheduledQueueExpectedStatus", (int)ReservationStatus.ScheduledQueueExpected);
            cmd.Parameters.AddWithValue(
                "@ExcludeReservationId",
                excludeReservationId.HasValue ? excludeReservationId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ReservedStartTime", reservedStartTime);
            cmd.Parameters.AddWithValue("@ReservedEndTime", reservedEndTime);

            connection.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
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
        private bool HasScheduledReservationConflict(
            string userKey,
            DateTime reservedStartTime,
            DateTime reservedEndTime,
            int? excludeReservationId = null)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Reservations
                WHERE UserId = @UserId
                  AND Status IN (@ScheduledStatus, @ScheduledQueueExpectedStatus)
                  AND (@ExcludeReservationId IS NULL OR Id <> @ExcludeReservationId)
                  AND ReservedStartTime < @ReservedEndTime
                  AND ReservedEndTime > @ReservedStartTime", connection);

            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@ScheduledStatus", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@ScheduledQueueExpectedStatus", (int)ReservationStatus.ScheduledQueueExpected);
            cmd.Parameters.AddWithValue(
                "@ExcludeReservationId",
                excludeReservationId.HasValue ? excludeReservationId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ReservedStartTime", reservedStartTime);
            cmd.Parameters.AddWithValue("@ReservedEndTime", reservedEndTime);

            connection.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
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

        // 這裡把未來預約建立前的推算邏輯集中在 Repository，
        // 讓真正寫入資料時也會用同一套規則，不會只在畫面查詢時看起來正確。
        private FutureReservationForecast BuildFutureReservationForecast(
            Equipment equipment,
            DateTime reservedStartTime,
            DateTime reservedEndTime,
            int? excludeReservationId = null)
        {
            var currentTaiwanTime = RepositorySqlHelper.GetTaiwanTime();
            var minutesUntilStart = Math.Max(0, (int)(reservedStartTime - currentTaiwanTime).TotalMinutes);

            var currentUsers = GetCurrentUsers(equipment.Id);
            var currentWaitingCount = GetCurrentWaitingCount(equipment.Id);
            var reservedCapacityCount = GetReservedCapacityCount(
                equipment.Id,
                reservedStartTime,
                reservedEndTime,
                excludeReservationId);

            if (reservedCapacityCount >= equipment.MaxUsers)
            {
                return new FutureReservationForecast
                {
                    HasReservedCapacityConflict = true,
                    ReservedCapacityCount = reservedCapacityCount,
                    ForecastWaitingCount = currentWaitingCount,
                    Message = "此時段的保留名額已滿，請改選其他時間"
                };
            }

            var effectiveCapacityAtTarget = Math.Max(0, equipment.MaxUsers - reservedCapacityCount);
            if (effectiveCapacityAtTarget <= 0)
            {
                return new FutureReservationForecast
                {
                    HasReservedCapacityConflict = true,
                    ReservedCapacityCount = reservedCapacityCount,
                    ForecastWaitingCount = currentWaitingCount,
                    Message = "此時段已無可再分配的容量，請改選其他時間"
                };
            }

            var availableStartsNow = Math.Max(0, equipment.MaxUsers - currentUsers);
            var fullCyclesBeforeSlot = equipment.AvailableTime <= 0
                ? 0
                : minutesUntilStart / equipment.AvailableTime;
            var theoreticalStartsBeforeSlot = availableStartsNow + (fullCyclesBeforeSlot * equipment.MaxUsers);
            var forecastWaitingCount = Math.Max(0, currentWaitingCount - theoreticalStartsBeforeSlot);

            if (forecastWaitingCount >= effectiveCapacityAtTarget)
            {
                return new FutureReservationForecast
                {
                    QueueExpected = true,
                    ReservedCapacityCount = reservedCapacityCount,
                    ForecastWaitingCount = forecastWaitingCount,
                    Message = $"依目前隊列推算，到這個時段時前面可能仍有 {forecastWaitingCount} 人在等待。若仍要預約，系統會在你確認後建立預約，並在到點時排入隊尾。"
                };
            }

            return new FutureReservationForecast
            {
                ReservedCapacityCount = reservedCapacityCount,
                ForecastWaitingCount = forecastWaitingCount,
                Message = $"此時段可預約，目前已保留名額 {reservedCapacityCount}/{equipment.MaxUsers}。"
            };
        }

        private int GetCurrentWaitingCount(byte equipmentId)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM WaitingQueue WHERE EquipmentId = @EquipmentId",
                connection);

            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            connection.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
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

        // 這個小工具專門給管理者調整未來預約使用。
        // 只抓仍處於 Scheduled / ScheduledQueueExpected 的資料，
        // 這樣可以避免誤把已開始或已結束的預約拿去調整時段。
        private Reservation? GetManageableScheduledReservation(int reservationId)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand(@"
                SELECT *
                FROM Reservations
                WHERE Id = @Id
                  AND Status IN (@ScheduledStatus, @ScheduledQueueExpectedStatus)", connection);
            cmd.Parameters.AddWithValue("@Id", reservationId);
            cmd.Parameters.AddWithValue("@ScheduledStatus", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@ScheduledQueueExpectedStatus", (int)ReservationStatus.ScheduledQueueExpected);

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
                EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("EndTime")),
                ReservationTime = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                Status = (ReservationStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                ReservedStartTime = reader.IsDBNull(reader.GetOrdinal("ReservedStartTime"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ReservedStartTime")),
                ReservedEndTime = reader.IsDBNull(reader.GetOrdinal("ReservedEndTime"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ReservedEndTime")),
                DurationMinutes = reader.IsDBNull(reader.GetOrdinal("DurationMinutes"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                ReservationType = reader.IsDBNull(reader.GetOrdinal("ReservationType"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("ReservationType"))
            };
        }

        // 這裡把「管理者調整時段前要做的檢查」集中起來，
        // 讓預覽與真正送出可以共用同一份判斷，不會一邊說可以、一邊寫入時才失敗。
        private RescheduleEvaluationResult EvaluateRescheduleScheduledReservation(
            AdminRescheduleReservationFormViewModel request)
        {
            var existingReservation = GetManageableScheduledReservation(request.ReservationId);
            if (existingReservation == null)
            {
                return RescheduleEvaluationResult.Fail("找不到可調整的未來預約，或該預約已經開始");
            }

            var equipment = GetEquipmentById(existingReservation.EquipmentId);
            if (equipment == null)
            {
                return RescheduleEvaluationResult.Fail("設備不存在");
            }

            if (!DateOnly.TryParse(request.ReservationDate, out var reservationDate))
            {
                return RescheduleEvaluationResult.Fail("新的預約日期格式不正確");
            }

            if (!TimeOnly.TryParse(request.SelectedSlotStartTime, out var slotStartTime))
            {
                return RescheduleEvaluationResult.Fail("新的預約時段格式不正確");
            }

            var taiwanTime = RepositorySqlHelper.GetTaiwanTime();
            var reservedStartTime = reservationDate.ToDateTime(slotStartTime);
            var reservedEndTime = reservedStartTime.AddMinutes(equipment.AvailableTime);
            var latestStartTime = reservationDate.ToDateTime(TimeOnly.MinValue)
                .Add(equipment.CloseTime)
                .AddMinutes(-equipment.AvailableTime);

            if (reservedStartTime <= taiwanTime)
            {
                return RescheduleEvaluationResult.Fail("新的預約時間必須晚於目前時間");
            }

            if (reservedStartTime.TimeOfDay < equipment.OpenTime || reservedStartTime > latestStartTime)
            {
                return RescheduleEvaluationResult.Fail("新的時段不在設備可預約範圍內");
            }

            if (HasScheduledReservationConflict(
                existingReservation.UserId,
                reservedStartTime,
                reservedEndTime,
                existingReservation.Id))
            {
                return RescheduleEvaluationResult.Fail("該會員在新的時段已有其他預約，請改選其他時間");
            }

            var forecast = BuildFutureReservationForecast(
                equipment,
                reservedStartTime,
                reservedEndTime,
                existingReservation.Id);

            if (forecast.HasReservedCapacityConflict)
            {
                return RescheduleEvaluationResult.Fail(forecast.Message);
            }

            return RescheduleEvaluationResult.Success(
                existingReservation,
                equipment,
                reservedStartTime,
                reservedEndTime,
                forecast,
                forecast.Message);
        }

        private ScheduledReservationItem CreateScheduledReservationItem(SqlDataReader reader)
        {
            var reservationId = reader.GetInt32(reader.GetOrdinal("Id"));
            var equipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId"));
            var reservedStartTime = reader.GetDateTime(reader.GetOrdinal("ReservedStartTime"));
            var reservedEndTime = reader.GetDateTime(reader.GetOrdinal("ReservedEndTime"));
            var status = reader.GetInt32(reader.GetOrdinal("Status"));
            var equipment = GetEquipmentById(equipmentId);

            var forecast = equipment == null
                ? new FutureReservationForecast
                {
                    Message = "無法取得設備資料，暫時無法估算風險摘要"
                }
                : BuildFutureReservationForecast(equipment, reservedStartTime, reservedEndTime, reservationId);

            return new ScheduledReservationItem
            {
                Id = reservationId,
                EquipmentId = equipmentId,
                EquipmentName = reader.GetString(reader.GetOrdinal("equipmentName")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                ReservationTime = reader.GetDateTime(reader.GetOrdinal("ReservationTime")),
                ReservedStartTime = reservedStartTime,
                ReservedEndTime = reservedEndTime,
                DurationMinutes = reader.IsDBNull(reader.GetOrdinal("DurationMinutes"))
                    ? 0
                    : reader.GetInt32(reader.GetOrdinal("DurationMinutes")),
                Status = status,
                StatusText = ReservationDisplayHelper.GetStatusText(status),
                StatusCssClass = ReservationDisplayHelper.GetStatusCssClass(status),
                QueueExpected = status == (int)ReservationStatus.ScheduledQueueExpected || forecast.QueueExpected,
                ReservedCapacityCount = forecast.ReservedCapacityCount,
                ForecastWaitingCount = forecast.ForecastWaitingCount,
                RiskSummary = BuildScheduledReservationRiskSummary(status, forecast)
            };
        }

        private static string BuildScheduledReservationRiskSummary(int status, FutureReservationForecast forecast)
        {
            if (status == (int)ReservationStatus.ScheduledQueueExpected || forecast.QueueExpected)
            {
                return $"目前已保留 {forecast.ReservedCapacityCount} 個名額，推算前方仍可能有 {forecast.ForecastWaitingCount} 人等待，屆時可能轉入排隊尾端。";
            }

            return $"目前已保留 {forecast.ReservedCapacityCount} 個名額，依現況推算可正常開始使用。";
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
                WHERE Status IN (@ScheduledStatus, @ScheduledQueueExpectedStatus)
                  AND ReservedStartTime IS NOT NULL
                  AND ReservedStartTime <= @CurrentTime
                ORDER BY ReservedStartTime, Id", connection);
            cmd.Parameters.AddWithValue("@ScheduledStatus", (int)ReservationStatus.Scheduled);
            cmd.Parameters.AddWithValue("@ScheduledQueueExpectedStatus", (int)ReservationStatus.ScheduledQueueExpected);
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

        private class RescheduleEvaluationResult
        {
            public bool CanProceed { get; private set; }
            public string Message { get; private set; } = string.Empty;
            public Reservation? ExistingReservation { get; private set; }
            public Equipment? Equipment { get; private set; }
            public DateTime ReservedStartTime { get; private set; }
            public DateTime ReservedEndTime { get; private set; }
            public FutureReservationForecast? Forecast { get; private set; }

            public static RescheduleEvaluationResult Fail(string message)
            {
                return new RescheduleEvaluationResult
                {
                    CanProceed = false,
                    Message = message
                };
            }

            public static RescheduleEvaluationResult Success(
                Reservation existingReservation,
                Equipment equipment,
                DateTime reservedStartTime,
                DateTime reservedEndTime,
                FutureReservationForecast forecast,
                string message)
            {
                return new RescheduleEvaluationResult
                {
                    CanProceed = true,
                    Message = message,
                    ExistingReservation = existingReservation,
                    Equipment = equipment,
                    ReservedStartTime = reservedStartTime,
                    ReservedEndTime = reservedEndTime,
                    Forecast = forecast
                };
            }
        }
    }
}
