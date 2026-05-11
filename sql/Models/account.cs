namespace sql.Models
{
#pragma warning disable CS8981
    public class account
    {
        public int id { get; set; }
        public string userName { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public double age { get; set; }

        // 目前資料表中的角色值仍以 user / manager / admin 為主。
        // 顯示時會再統一轉成「普通會員 / 管理員」。
        public string role { get; set; } = "user";

        public string email { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public bool isActive { get; set; } = true;
    }

    public class AccountLoginResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public account? User { get; set; }
    }
#pragma warning restore CS8981
}
