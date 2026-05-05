using Microsoft.AspNetCore.Http;

namespace sql.Services
{
    // 這個 Service 專門負責整理「目前登入者」資訊。
    // 以前每個地方自己讀 Session，久了會很亂；現在統一集中在這裡。
    public class CurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // 把 Session 中的欄位整理成一個比較好用的 CurrentUser 物件。
        // 新手可以把它想成：把分散的資料組成一張完整的登入者資料卡。
        public CurrentUser GetCurrentUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return CurrentUser.Anonymous;
            }

            var userId = httpContext.Session.GetInt32("UserId");
            var userName = httpContext.Session.GetString("UserName");
            var userRole = httpContext.Session.GetString("UserRole") ?? "user";

            if (userId == null || string.IsNullOrEmpty(userName))
            {
                return CurrentUser.Anonymous;
            }

            return new CurrentUser
            {
                UserId = userId.Value,
                UserName = userName,
                Role = userRole
            };
        }

        public bool IsAuthenticated()
        {
            return GetCurrentUser().IsAuthenticated;
        }

        public bool IsManager()
        {
            return GetCurrentUser().IsManager;
        }
    }

    // 這個類別不是資料表，而是程式執行中的登入者模型。
    public class CurrentUser
    {
        public static CurrentUser Anonymous => new();

        public int? UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string Role { get; init; } = "user";

        // 把常用判斷包成屬性，避免每次都自己寫一長串 if。
        public bool IsAuthenticated => UserId.HasValue && !string.IsNullOrEmpty(UserName);
        public bool IsManager => Role == "manager" || Role == "admin";

        // 目前是過渡做法：
        // 雖然長期目標要改成會員主鍵，但舊資料仍以 UserName 當預約 / 排隊識別。
        // 先集中成一個屬性，之後要改時只要改少數地方。
        public string ReservationUserKey => UserName;
    }
}
