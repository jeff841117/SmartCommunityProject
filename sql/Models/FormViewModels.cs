using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 這份檔案集中管理表單輸入模型。
    // 好處是欄位驗證、錯誤訊息、畫面輸入邊界都放在同一處，
    // 之後 Controller 只需要接模型，不必再自己拆一堆散參數。
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
        [Required(ErrorMessage = "請輸入帳號名稱")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; } = string.Empty;

        [Range(1, 200, ErrorMessage = "請輸入合理年齡")]
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

        [Range(1, 100, ErrorMessage = "請輸入合理的同時使用上限")]
        public byte MaxUsers { get; set; }

        [Range(1, 1440, ErrorMessage = "請輸入合理的使用時間（分鐘）")]
        public short AvailableTime { get; set; }

        [Required(ErrorMessage = "請輸入開放時間")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "請輸入關閉時間")]
        public TimeSpan CloseTime { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class UpdateEquipmentFormViewModel
    {
        [Required(ErrorMessage = "缺少設備編號")]
        public byte Id { get; set; }

        [Required(ErrorMessage = "請輸入設備名稱")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇設備種類")]
        public string EquipmentCategory { get; set; } = "場館";

        [Range(1, 100, ErrorMessage = "請輸入合理的同時使用上限")]
        public byte MaxUsers { get; set; }

        [Range(1, 1440, ErrorMessage = "請輸入合理的使用時間（分鐘）")]
        public short AvailableTime { get; set; }

        [Required(ErrorMessage = "請輸入開放時間")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "請輸入關閉時間")]
        public TimeSpan CloseTime { get; set; }
    }

    public class DeleteEquipmentFormViewModel
    {
        [Required(ErrorMessage = "缺少設備編號")]
        public byte Id { get; set; }
    }

    public class UpdateAccountFormViewModel
    {
        [Required(ErrorMessage = "缺少會員編號")]
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
        [StringLength(6, MinimumLength = 6, ErrorMessage = "驗證碼必須是 6 碼")]
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

    // 這個模型專門給管理者後台調整未來預約時段。
    // 我們把日期、時段與是否接受預約排隊拆成獨立欄位，
    // 讓後端可以直接沿用既有的未來預約推算規則。
    public class AdminRescheduleReservationFormViewModel
    {
        [Required(ErrorMessage = "缺少預約編號")]
        public int ReservationId { get; set; }

        [Required(ErrorMessage = "請選擇新的預約日期")]
        public string ReservationDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇新的開始時段")]
        public string SelectedSlotStartTime { get; set; } = string.Empty;

        public bool ConfirmQueueExpected { get; set; }
    }
}
