namespace sql.Models
{
    // 這些回應模型是給 Controller 回 JSON 時使用的共用格式。
    // 目的不是增加檔案數量，而是把原本零散的匿名物件整理成可追蹤的型別。
    // 這樣之後要 API 化、補欄位、或查找誰用了哪些回傳格式時會輕鬆很多。

    // 最常見的操作結果格式：成功 / 失敗 + 訊息。
    public class ApiOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // 有些管理功能在失敗時會想帶更多資訊，
    // 例如資料庫錯誤代碼或開發階段的 stack trace。
    // 這時候就用這個稍微詳細一點的版本。
    public class ApiDetailedOperationResponse : ApiOperationResponse
    {
        public int? ErrorCode { get; set; }
        public string? StackTrace { get; set; }
    }

    // 需要包一層資料內容時使用。
    // 新手可以把它理解成：
    // success / message 是「外層狀態」，
    // data 是「真正想交給前端的內容」。
    public class ApiDataResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    // 舊頁面現在不少地方還是習慣接 error 欄位，
    // 所以先保留這種形狀，避免前端一起大改。
    public class ApiErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
    }

    // 給前端快速判斷目前登入狀態的資料。
    public class CurrentUserResponse
    {
        public bool IsLoggedIn { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    // 給簡化版設備可用性檢查使用。
    public class AvailabilitySummaryResponse
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // 測試 / 偵錯頁面用的資料格式檢查回應。
    public class ReservationDataFormatResponse
    {
        public int ActiveReservationsCount { get; set; }
        public int WaitingReservationsCount { get; set; }
        public int HistoryReservationsCount { get; set; }
        public object? SampleActive { get; set; }
        public object? SampleWaiting { get; set; }
        public object? SampleHistory { get; set; }
        public ReservationDataTypesInfo DataTypes { get; set; } = new();
    }

    public class ReservationDataTypesInfo
    {
        public string ActiveType { get; set; } = string.Empty;
        public string WaitingType { get; set; } = string.Empty;
        public string HistoryType { get; set; } = string.Empty;
    }
}
