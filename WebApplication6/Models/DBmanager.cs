using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApplication6;

namespace 系統端.Models
{
    public class DBmanager
    {
        private readonly string connStr = "Data Source=(localdb)\\MSSQLLocalDB;Database=delivery;User ID=harry;Password=vupaua1831129;Trusted_Connection=True";
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

