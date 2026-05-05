namespace sql.Models
{
#pragma warning disable CS8981
    public class account
    {
        public int id { get; set; }
        public string userName { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public double age { get; set; }

        // 預設建立的新帳號都先視為一般會員。
        public string role { get; set; } = "user";

        public string email { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
    }
#pragma warning restore CS8981
}
