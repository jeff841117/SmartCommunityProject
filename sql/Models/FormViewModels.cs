using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 這份檔案集中管理前後台表單輸入模型。
    // 先把欄位邊界寫清楚，Controller 才不用自己手刻 if / else 驗證。
    public class LoginFormViewModel
    {
        [Required(ErrorMessage = "請輸入帳號")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AddAccountFormViewModel
    {
        [Required(ErrorMessage = "請輸入新增帳號")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; } = string.Empty;

        [Range(1, 200, ErrorMessage = "年齡需介於 1 到 200 歲")]
        public double Age { get; set; }

        [EmailAddress(ErrorMessage = "請輸入正確的 Email 格式")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AddEquipmentFormViewModel
    {
        [Required(ErrorMessage = "請輸入設備名稱")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇設備種類")]
        public string EquipmentCategory { get; set; } = "場館";

        [Range(1, 100, ErrorMessage = "同時上限人數需介於 1 到 100 之間")]
        public byte MaxUsers { get; set; }

        [Range(1, 1440, ErrorMessage = "可使用時間需介於 1 到 1440 分鐘之間")]
        public short AvailableTime { get; set; }

        [Required(ErrorMessage = "請輸入開放時間")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "請輸入關閉時間")]
        public TimeSpan CloseTime { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class UpdateEquipmentFormViewModel
    {
        [Required(ErrorMessage = "請提供設備編號")]
        public byte Id { get; set; }

        [Required(ErrorMessage = "請輸入設備名稱")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇設備種類")]
        public string EquipmentCategory { get; set; } = "場館";

        // 舊資料可能已經存在超過 100 的人數設定。
        // 這裡先放寬到 byte 上限，避免管理者只想修改設備種類時整筆被擋下。
        [Range(1, 255, ErrorMessage = "同時上限人數需介於 1 到 255 之間")]
        public byte MaxUsers { get; set; }

        // 舊資料可能已有 0 分鐘紀錄，先允許更新通過，
        // 後續再視驗收結果決定是否統一清理成更嚴格規則。
        [Range(0, 1440, ErrorMessage = "可使用時間需介於 0 到 1440 分鐘之間")]
        public short AvailableTime { get; set; }

        [Required(ErrorMessage = "請輸入開放時間")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "請輸入關閉時間")]
        public TimeSpan CloseTime { get; set; }
    }

    public class DeleteEquipmentFormViewModel
    {
        [Required(ErrorMessage = "請提供設備編號")]
        public byte Id { get; set; }
    }

    public class UpdateAccountFormViewModel
    {
        [Required(ErrorMessage = "請提供帳號編號")]
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "請輸入正確的 Email 格式")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
    }

    public class ForgotPasswordFormViewModel
    {
        [Required(ErrorMessage = "請輸入註冊時設定的 Email")]
        [EmailAddress(ErrorMessage = "請輸入正確的 Email 格式")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入 6 碼驗證碼")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "驗證碼需為 6 碼")]
        public string VerificationCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入新密碼")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "請再次輸入新密碼")]
        [Compare(nameof(NewPassword), ErrorMessage = "兩次輸入的新密碼不一致")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Step { get; set; } = "request";
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public string DebugCode { get; set; } = string.Empty;
    }

    // 這個模型讓管理者調整未來預約時段時，
    // 可以把必要欄位收斂成一個固定的輸入邊界。
    public class AdminRescheduleReservationFormViewModel
    {
        [Required(ErrorMessage = "請提供預約編號")]
        public int ReservationId { get; set; }

        [Required(ErrorMessage = "請選擇新的預約日期")]
        public string ReservationDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇新的預約時間")]
        public string SelectedSlotStartTime { get; set; } = string.Empty;

        public bool ConfirmQueueExpected { get; set; }
    }
}
