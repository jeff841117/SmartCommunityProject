using Microsoft.Data.SqlClient;
using sql.Models;
using sql.Services;

namespace sql.Repositories
{
    // QueueRepository 專門處理排隊資料表的讀寫。
    // 目標是讓 Service 不需要直接碰 SQL。
    public class QueueRepository
    {
        private readonly DBmanager _dbManager;
        private readonly EquipmentStateNotifier _equipmentStateNotifier;

        public QueueRepository(
            DBmanager dbManager,
            EquipmentStateNotifier equipmentStateNotifier)
        {
            _dbManager = dbManager;
            _equipmentStateNotifier = equipmentStateNotifier;
        }

        public void ProcessAllQueues()
        {
            // 先統一補做一次過期處理，避免排隊判斷拿到舊的使用中資料。
            CompleteExpiredReservations();

            foreach (var equipmentId in GetAllEquipmentIds())
            {
                ProcessEquipmentQueue(equipmentId);
            }
        }

        public void ProcessEquipmentQueue(byte equipmentId)
        {
            try
            {
                CompleteExpiredReservations();

                var equipment = GetEquipmentById(equipmentId);
                if (equipment == null)
                {
                    return;
                }

                using var connection = _dbManager.CreateConnection();
                connection.Open();

                // 這裡的核心想法很簡單：
                // 只要設備還有空位，而且還有人在排隊，就持續把最前面的人補上去。
                var currentUsers = GetCurrentUsers(connection, equipmentId);
                var waitingList = GetWaitingQueue(connection, equipmentId);

                while (currentUsers < equipment.MaxUsers && waitingList.Count > 0)
                {
                    var nextInQueue = waitingList[0];
                    var promoted = false;

                    // 補位前先再確認一次，避免排隊者其實已經有相同設備的使用中預約。
                    if (!HasActiveReservationForSameEquipment(connection, equipmentId, nextInQueue.UserId))
                    {
                        var taiwanTime = RepositorySqlHelper.GetTaiwanTime();
                        var reservation = new Reservation
                        {
                            EquipmentId = equipmentId,
                            UserId = nextInQueue.UserId,
                            StartTime = taiwanTime,
                            ReservationTime = taiwanTime,
                            Status = ReservationStatus.InProgress
                        };

                        InsertReservation(connection, reservation);
                        currentUsers++;
                        promoted = true;
                    }

                    RemoveFromQueue(connection, nextInQueue.Id);
                    RecalculateQueuePositions(connection, equipmentId);
                    waitingList = GetWaitingQueue(connection, equipmentId);

                    // 新手可以把這裡想成「同步畫面狀態」：
                    // 先把資料庫補位完成，再通知觀察者更新目前人數與排隊資訊。
                    _equipmentStateNotifier.Notify(equipmentId);

                    if (promoted)
                    {
                        NotifyUserPromotedFromQueue(nextInQueue.UserId, equipment.equipmentName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"處理設備 {equipmentId} 排隊時發生錯誤: {ex.Message}");
            }
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
            return GetCurrentUsers(connection, equipmentId);
        }

        // 讀取指定設備的排隊清單。
        // 依 QueueTime 排序，代表越早排隊的人越前面。
        public List<WaitingQueue> GetWaitingQueue(byte equipmentId)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();
            return GetWaitingQueue(connection, equipmentId);
        }

        // 取消排隊不是只有刪資料而已。
        // 真正的流程是：
        // 1. 先確認這筆資料是目前這位使用者的
        // 2. 刪除排隊記錄
        // 3. 重算順位
        // 4. 再檢查有沒有人現在可以遞補上設備
        public bool CancelQueue(int queueId, string userKey)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            using var checkCmd = new SqlCommand(
                "SELECT EquipmentId FROM WaitingQueue WHERE Id = @Id AND UserId = @UserId",
                connection);
            checkCmd.Parameters.AddWithValue("@Id", queueId);
            checkCmd.Parameters.AddWithValue("@UserId", userKey);

            var result = checkCmd.ExecuteScalar();
            if (result == null)
            {
                return false;
            }

            var equipmentId = (byte)result;

            using var deleteCmd = new SqlCommand(
                "DELETE FROM WaitingQueue WHERE Id = @Id AND UserId = @UserId",
                connection);
            deleteCmd.Parameters.AddWithValue("@Id", queueId);
            deleteCmd.Parameters.AddWithValue("@UserId", userKey);

            var rowsAffected = deleteCmd.ExecuteNonQuery();
            if (rowsAffected <= 0)
            {
                return false;
            }

            // 新手可以把這裡想成：有人離隊後，後面的人順位要往前補。
            RecalculateQueuePositions(connection, equipmentId);

            // 排序更新完後，再檢查這個設備是否有人可以立即補上。
            ProcessEquipmentQueue(equipmentId);
            return true;
        }

        // 重算排隊順位的做法：
        // 先依加入時間抓出所有 Id，再按照順序更新成 1,2,3...
        private static void RecalculateQueuePositions(SqlConnection connection, byte equipmentId)
        {
            using var selectCmd = new SqlCommand(
                "SELECT Id FROM WaitingQueue WHERE EquipmentId = @EquipmentId ORDER BY QueueTime",
                connection);
            selectCmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

            var queueIds = new List<int>();
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    queueIds.Add(reader.GetInt32(0));
                }
            }

            for (var i = 0; i < queueIds.Count; i++)
            {
                using var updateCmd = new SqlCommand(
                    "UPDATE WaitingQueue SET Position = @Position WHERE Id = @Id",
                    connection);
                updateCmd.Parameters.AddWithValue("@Position", i + 1);
                updateCmd.Parameters.AddWithValue("@Id", queueIds[i]);
                updateCmd.ExecuteNonQuery();
            }
        }

        // 這個方法只抓設備主鍵，給批次處理所有設備排隊時使用。
        private List<byte> GetAllEquipmentIds()
        {
            var equipmentIds = new List<byte>();

            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand("SELECT Id FROM Equipment ORDER BY Id", connection);
            connection.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                equipmentIds.Add(reader.GetByte(0));
            }

            return equipmentIds;
        }

        private int GetCurrentUsers(SqlConnection connection, byte equipmentId)
        {
            return RepositorySqlHelper.GetCurrentUsers(connection, equipmentId);
        }

        private List<WaitingQueue> GetWaitingQueue(SqlConnection connection, byte equipmentId)
        {
            var queue = new List<WaitingQueue>();

            using var cmd = new SqlCommand(
                "SELECT * FROM WaitingQueue WHERE EquipmentId = @EquipmentId ORDER BY QueueTime",
                connection);
            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);

            using var reader = cmd.ExecuteReader();
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

            return queue;
        }

        private bool HasActiveReservationForSameEquipment(SqlConnection connection, byte equipmentId, string userKey)
        {
            using var cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Reservations
                WHERE EquipmentId = @EquipmentId
                  AND UserId = @UserId
                  AND Status = @Status", connection);
            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            cmd.Parameters.AddWithValue("@UserId", userKey);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private void InsertReservation(SqlConnection connection, Reservation reservation)
        {
            RepositorySqlHelper.InsertReservation(connection, reservation);
        }

        private static void RemoveFromQueue(SqlConnection connection, int queueId)
        {
            using var cmd = new SqlCommand("DELETE FROM WaitingQueue WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", queueId);
            cmd.ExecuteNonQuery();
        }

        // 被補位成功的通知目前先保留成主控台輸出，
        // 後面如果你要接 Email、站內通知、WebSocket，就從這裡往外擴充。
        private static void NotifyUserPromotedFromQueue(string userId, string equipmentName)
        {
            Console.WriteLine($"通知：用戶 {userId} 已從 {equipmentName} 的排隊中晉升為使用中");
        }

        // QueueRepository 先自行補做過期清理，
        // 這樣排隊流程就不需要再回跳 LegacyReservationFlowBridge 的過期處理。
        private void CompleteExpiredReservations()
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();

            var expiredReservations = GetExpiredReservations(connection);
            if (!expiredReservations.Any())
            {
                return;
            }

            foreach (var reservation in expiredReservations)
            {
                using var updateCmd = new SqlCommand(@"
                    UPDATE Reservations
                    SET Status = @CompletedStatus, EndTime = @EndTime
                    WHERE Id = @Id", connection);
                updateCmd.Parameters.AddWithValue("@CompletedStatus", (int)ReservationStatus.Completed);
                updateCmd.Parameters.AddWithValue("@EndTime", RepositorySqlHelper.GetTaiwanTime());
                updateCmd.Parameters.AddWithValue("@Id", reservation.Id);
                updateCmd.ExecuteNonQuery();

                _equipmentStateNotifier.Notify(reservation.EquipmentId);
            }
        }

        private static List<ExpiredQueueReservationInfo> GetExpiredReservations(SqlConnection connection)
        {
            var expiredReservations = new List<ExpiredQueueReservationInfo>();

            using var selectCmd = new SqlCommand(@"
                SELECT r.Id, r.EquipmentId
                FROM Reservations r
                INNER JOIN Equipment e ON r.EquipmentId = e.Id
                WHERE r.Status = @Status
                  AND DATEADD(MINUTE, e.AvailableTime, r.StartTime) <= @CurrentTime", connection);
            selectCmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);
            selectCmd.Parameters.AddWithValue("@CurrentTime", RepositorySqlHelper.GetTaiwanTime());

            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                expiredReservations.Add(new ExpiredQueueReservationInfo
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    EquipmentId = reader.GetByte(reader.GetOrdinal("EquipmentId"))
                });
            }

            return expiredReservations;
        }

        private class ExpiredQueueReservationInfo
        {
            public int Id { get; set; }
            public byte EquipmentId { get; set; }
        }

    }
}
