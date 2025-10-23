using Microsoft.Data.SqlClient;

namespace Delivery_user.Models
{
    public class DB
    {
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Database=delivery;User ID=harry;Password=vupaua1831129";
        public List<Package> GetPendingPackagesByPID(string pid)
        {
            if (string.IsNullOrEmpty(pid))
                return new List<Package>();

            List<Package> pendingPackages = new List<Package>();

            using (SqlConnection sqlConnection = new SqlConnection(connStr))
            {
                string sql = "SELECT * FROM Package WHERE PID = @pid AND STA = 0"; // 不要加 (@pid nvarchar(4000))
                SqlCommand sqlCommand = new SqlCommand(sql, sqlConnection);
                sqlCommand.Parameters.Add(new SqlParameter("@pid", pid)); // 正確傳入參數

                sqlConnection.Open();
                SqlDataReader reader = sqlCommand.ExecuteReader();

                while (reader.Read())
                {
                    pendingPackages.Add(new Package
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("id")),
                        PID = reader.GetString(reader.GetOrdinal("pid")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        PhoneNumber = reader.GetString(reader.GetOrdinal("phonenumber")),
                        STA = reader.GetBoolean(reader.GetOrdinal("STA")),
                        Date = reader.GetDateTime(reader.GetOrdinal("Date"))
                    });
                }

                sqlConnection.Close();
            }

            return pendingPackages;
        }


        public List<Details> getDetails()
        {
            List<Details> detailsList = new List<Details>();

            SqlConnection sqlConnection = new SqlConnection(connStr);
            SqlCommand sqlCommand = new SqlCommand("SELECT * FROM PackageDetail");
            sqlCommand.Connection = sqlConnection;
            sqlConnection.Open();

            SqlDataReader reader = sqlCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Details detail = new Details
                    {
                        
                        PackageID = reader.GetInt32(reader.GetOrdinal("PackageID")),
                        SenderName = reader.GetString(reader.GetOrdinal("SenderName")),
                        SenderPhn = reader.GetString(reader.GetOrdinal("SenderPhn")),
                        SenderAddress = reader.GetString(reader.GetOrdinal("SenderAddress")),
                        RecipientName = reader.GetString(reader.GetOrdinal("RecipientName")),
                        RecipientPhn = reader.GetString(reader.GetOrdinal("RecipientPhn")),
                        RecipientAddress = reader.GetString(reader.GetOrdinal("RecipientAddress")),
                        SendDate = reader.GetDateTime(reader.GetOrdinal("SendDate")),
                        DeliveryStatus = reader.GetString(reader.GetOrdinal("DeliveryStatus")),
                        CreateDate = reader.GetDateTime(reader.GetOrdinal("CreateDate")),
                        UpdateDate = reader.GetDateTime(reader.GetOrdinal("UpdateDate")),
                    };
                    detailsList.Add(detail);
                }
            }
            else
            {
                Console.WriteLine("資料庫為空！");
            }
            sqlConnection.Close();
            return detailsList;
        }

        public void InsertDelivery(string SenderName, string SenderPhn, string SenderAddress, string RecipientName, string RecipientPhn, string RecipientAddress, DateTime SendDate)
        {
            SqlConnection sqlconnection = new SqlConnection(connStr);
            SqlCommand sqlcommand = new SqlCommand(@"INSERT INTO PackageDetail(SenderName,SenderPhn,SenderAddress,
             RecipientName,RecipientPhn,RecipientAddress,SendDate,DeliveryStatus,CreateDate,UpdateDate) 
             VALUES(@senderName,@senderPhn,@senderAddress,@recipientName,@recipientPhn,@recipientAddress,@sendDate,'Pending',GETDATE(),GETDATE())");
            sqlcommand.Connection = sqlconnection;

            sqlcommand.Parameters.Add(new SqlParameter("@senderName", SenderName));
            sqlcommand.Parameters.Add(new SqlParameter("@senderPhn", SenderPhn));
            sqlcommand.Parameters.Add(new SqlParameter("@senderAddress", SenderAddress));
            sqlcommand.Parameters.Add(new SqlParameter("@recipientName", RecipientName));
            sqlcommand.Parameters.Add(new SqlParameter("@recipientPhn", RecipientPhn));
            sqlcommand.Parameters.Add(new SqlParameter("@recipientAddress", RecipientAddress));
            sqlcommand.Parameters.Add(new SqlParameter("@sendDate", SendDate));

            sqlconnection.Open();
            sqlcommand.ExecuteNonQuery();
            sqlconnection.Close();
        }
        //public void InsertPackage(Package package, string userLineId)
        //{
        //    using (SqlConnection conn = new SqlConnection(connStr))
        //    {
        //        string sql = @"INSERT INTO Package (Name, PhoneNumber, STA, Date)
        //               VALUES (@Name, @PhoneNumber, @STA, @Date);
        //               SELECT CAST(SCOPE_IDENTITY() AS INT)";
        //        SqlCommand cmd = new SqlCommand(sql, conn);
        //        cmd.Parameters.AddWithValue("@Name", package.Name);
        //        cmd.Parameters.AddWithValue("@PhoneNumber", package.PhoneNumber);
        //        cmd.Parameters.AddWithValue("@STA", package.STA);
        //        cmd.Parameters.AddWithValue("@Date", package.Date);

        //        conn.Open();
        //        int newId = (int)cmd.ExecuteScalar(); // 取得新包裹編號
        //        package.ID = newId;

        //        // ---- 發送 LINE 訊息 ----
        //        var lineService = new LineService();
        //        string msg = $"您好，您有新包裹待領取\n包裹編號：{newId}\n請盡快領取。";
        //        Task.Run(async () => await lineService.PushMessageAsync(userLineId, msg));
        //    }
    }
}
