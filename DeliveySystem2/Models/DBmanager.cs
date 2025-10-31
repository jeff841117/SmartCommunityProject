using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace DeliveySystem2.Models
{
    public class DBmanager
    {
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=delivery;User ID=harry;Password=vupaua1831129;Trusted_Connection=True";
        public string CheckUser(string account, string password)
        {
            string role = null;
            using (SqlConnection conn = new SqlConnection(connStr))
            {

                string sql = "SELECT role FROM UserAccount WHERE account = @account AND pwd = @password";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@account", account);
                cmd.Parameters.AddWithValue("@password", password);
                conn.Open();

                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    role = result.ToString();
                }
            }
            return role;
        }
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
        public void newPackage(Package user)
        {
            SqlConnection sqlconnection = new SqlConnection(connStr);
            SqlCommand sqlcommand = new SqlCommand(@"INSERT INTO Package(PID,Name,PhoneNumber,STA) VALUES(@PID,@Name,@PhoneNumber,@STA)");
            sqlcommand.Connection = sqlconnection;

            sqlcommand.Parameters.Add(new SqlParameter("@PID", user.PID));
            sqlcommand.Parameters.Add(new SqlParameter("@Name", user.Name));
            sqlcommand.Parameters.Add(new SqlParameter("@PhoneNumber", user.PhoneNumber));
            sqlcommand.Parameters.Add(new SqlParameter("@STA", user.STA));

            sqlconnection.Open();
            sqlcommand.ExecuteNonQuery();
            sqlconnection.Close();
        }

        public List<Package> getPackages()
        {
            List<Package> packages = new List<Package>();

            SqlConnection sqlConnection = new SqlConnection(connStr);
            SqlCommand sqlCommand = new SqlCommand("SELECT * FROM Package");
            sqlCommand.Connection = sqlConnection;
            sqlConnection.Open();

            SqlDataReader reader = sqlCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Package package = new Package
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("id")),
                        PID = reader.GetString(reader.GetOrdinal("pid")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        PhoneNumber = reader.GetString(reader.GetOrdinal("phonenumber")),
                        STA = reader.GetBoolean(reader.GetOrdinal("STA")),
                        Date = reader.GetDateTime(reader.GetOrdinal("date"))
                    };
                    packages.Add(package);
                }
            }
            else
            {
                Console.WriteLine("資料庫為空！");
            }
            sqlConnection.Close();
            return packages;
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
        public void UpdatePackageStatus(int id, bool status)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string sql = "UPDATE Package SET STA = @STA WHERE ID = @ID";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@STA", status);
                cmd.Parameters.AddWithValue("@ID", id);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}