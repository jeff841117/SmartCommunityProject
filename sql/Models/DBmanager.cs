using Microsoft.Data.SqlClient;

namespace sql.Models
{
    public class DBmanager
    {
        // 目前 DBmanager 已從「超大型資料操作類」收斂成「資料庫連線工廠」。
        // 真正的帳號、設備、預約、排隊 SQL 都已逐步搬到各自的 Repository。
        // 先保留這個類別，是為了讓既有 DI 與 Repository 還能共用同一個連線設定。
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=sql_db_test;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;";

        // 新手可以把這個方法理解成：
        // 「每次 Repository 要查資料庫時，先來這裡領一條新的資料庫連線」。
        // 這樣連線字串只需要集中管理一份，之後如果要切換正式庫或測試庫，也比較好調整。
        public SqlConnection CreateConnection()
        {
            return new SqlConnection(connStr);
        }
    }
}
