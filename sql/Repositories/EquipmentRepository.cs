using Microsoft.Data.SqlClient;
using sql.Models;

namespace sql.Repositories
{
    // EquipmentRepository 專門處理設備資料表的讀寫。
    // 這樣 Controller 與 Service 就不需要直接寫 SQL。
    public class EquipmentRepository
    {
        private readonly DBmanager _dbManager;

        public EquipmentRepository(DBmanager dbManager)
        {
            _dbManager = dbManager;
        }

        public List<Equipment> GetAll()
        {
            var equipments = new List<Equipment>();

            using var connection = _dbManager.CreateConnection();
            EnsureEquipmentCategoryColumn(connection);
            using var cmd = new SqlCommand("SELECT * FROM Equipment", connection);

            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                equipments.Add(RepositorySqlHelper.MapEquipment(reader));
            }

            return equipments;
        }

        public Equipment? GetById(byte equipmentId)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureEquipmentCategoryColumn(connection);
            using var cmd = new SqlCommand("SELECT * FROM Equipment WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", equipmentId);

            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return RepositorySqlHelper.MapEquipment(reader);
        }

        public void Create(Equipment equipment)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureEquipmentCategoryColumn(connection);
            using var cmd = new SqlCommand(@"
                INSERT INTO Equipment (equipmentName, EquipmentCategory, MaxUsers, AvailableTime, OpenTime, CloseTime)
                VALUES (@equipmentName, @EquipmentCategory, @MaxUsers, @AvailableTime, @OpenTime, @CloseTime)", connection);

            FillEquipmentParameters(cmd, equipment, includeId: false);
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            cmd.ExecuteNonQuery();
        }

        public void Update(Equipment equipment)
        {
            using var connection = _dbManager.CreateConnection();
            EnsureEquipmentCategoryColumn(connection);
            using var cmd = new SqlCommand(@"
                UPDATE Equipment
                SET equipmentName = @equipmentName,
                    EquipmentCategory = @EquipmentCategory,
                    MaxUsers = @MaxUsers,
                    AvailableTime = @AvailableTime,
                    OpenTime = @OpenTime,
                    CloseTime = @CloseTime
                WHERE Id = @Id", connection);

            FillEquipmentParameters(cmd, equipment, includeId: true);
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
            cmd.ExecuteNonQuery();
        }

        public void Delete(byte equipmentId)
        {
            using var connection = _dbManager.CreateConnection();
            using var cmd = new SqlCommand("DELETE FROM Equipment WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", equipmentId);

            connection.Open();
            cmd.ExecuteNonQuery();
        }

        public int GetCurrentUsers(byte equipmentId)
        {
            using var connection = _dbManager.CreateConnection();
            connection.Open();
            return RepositorySqlHelper.GetCurrentUsers(connection, equipmentId);
        }

        // 把新增 / 修改共用的參數填充集中在一起，避免重複程式碼。
        private static void FillEquipmentParameters(SqlCommand cmd, Equipment equipment, bool includeId)
        {
            cmd.Parameters.AddWithValue("@equipmentName", equipment.equipmentName);
            cmd.Parameters.AddWithValue("@EquipmentCategory", equipment.EquipmentCategory);
            cmd.Parameters.AddWithValue("@MaxUsers", equipment.MaxUsers);
            cmd.Parameters.AddWithValue("@AvailableTime", equipment.AvailableTime);
            cmd.Parameters.AddWithValue("@OpenTime", equipment.OpenTime);
            cmd.Parameters.AddWithValue("@CloseTime", equipment.CloseTime);

            if (includeId)
            {
                cmd.Parameters.AddWithValue("@Id", equipment.Id);
            }
        }

        // 這裡用最小成本補資料表欄位，避免測試環境或舊資料庫還沒跑 migration 時，
        // 前台設備種類篩選一打開就直接因為缺欄位失敗。
        private static void EnsureEquipmentCategoryColumn(SqlConnection connection)
        {
            var wasClosed = connection.State != System.Data.ConnectionState.Open;
            if (wasClosed)
            {
                connection.Open();
            }

            using var cmd = new SqlCommand(@"
                IF COL_LENGTH('Equipment', 'EquipmentCategory') IS NULL
                BEGIN
                    ALTER TABLE Equipment
                    ADD EquipmentCategory NVARCHAR(20) NOT NULL
                        CONSTRAINT DF_Equipment_EquipmentCategory DEFAULT N'場館';

                    UPDATE Equipment
                    SET EquipmentCategory = N'場館'
                    WHERE EquipmentCategory IS NULL;
                END", connection);

            cmd.ExecuteNonQuery();
        }
    }
}
