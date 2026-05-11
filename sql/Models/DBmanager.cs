using Microsoft.Data.SqlClient;

namespace sql.Models
{
    public class DBmanager
    {
        // DBmanager 現在只保留「建立連線」這個責任。
        // 舊版原本把大量 SQL 都放在這裡，但目前已逐步搬到各個 Repository。
        // 這樣之後要測試、重構或更換資料來源時，影響範圍會比較小。
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=sql_db_test;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;";

        // 提供 Repository 共用的 SQL 連線工廠。
        // 目前專案的資料存取都應該優先走 Repository，再由 Repository 透過這裡開連線。
        public SqlConnection CreateConnection()
        {
            return new SqlConnection(connStr);
        }
    }
}
