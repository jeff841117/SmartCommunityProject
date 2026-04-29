using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // 這個 helper 用來集中 repository 之間會重複使用的小型查詢與映射。
    // 好處是：
    // 1. 避免每個 repository 都複製一份很像的 SQL
    // 2. 後面若欄位調整，只要集中改少數地方
    internal static class RepositorySqlHelper
    {
        public static Equipment? GetEquipmentById(SqlConnection connection, byte equipmentId)
        {
            using var cmd = new SqlCommand("SELECT * FROM Equipment WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", equipmentId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapEquipment(reader);
        }

        public static int GetCurrentUsers(SqlConnection connection, byte equipmentId)
        {
            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Reservations WHERE EquipmentId = @EquipmentId AND Status = @Status",
                connection);
            cmd.Parameters.AddWithValue("@EquipmentId", equipmentId);
            cmd.Parameters.AddWithValue("@Status", (int)ReservationStatus.InProgress);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // 這一輪把等待中的名單也收進 helper，
        // 讓像 notifier 這種非 repository 元件也能安全重用同一套查詢。
        public static List<WaitingQueue> GetWaitingQueue(SqlConnection connection, byte equipmentId)
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

        public static void InsertReservation(SqlConnection connection, Reservation reservation)
        {
            using var cmd = new SqlCommand(
                "INSERT INTO Reservations (EquipmentId, UserId, StartTime, ReservationTime, Status) VALUES (@EquipmentId, @UserId, @StartTime, @ReservationTime, @Status)",
                connection);
            cmd.Parameters.AddWithValue("@EquipmentId", reservation.EquipmentId);
            cmd.Parameters.AddWithValue("@UserId", reservation.UserId);
            cmd.Parameters.AddWithValue("@StartTime", reservation.StartTime);
            cmd.Parameters.AddWithValue("@ReservationTime", reservation.ReservationTime);
            cmd.Parameters.AddWithValue("@Status", (int)reservation.Status);
            cmd.ExecuteNonQuery();
        }

        public static DateTime GetTaiwanTime()
        {
            try
            {
                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            }
            catch
            {
                return DateTime.UtcNow.AddHours(8);
            }
        }

        public static Equipment MapEquipment(SqlDataReader reader)
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
}
