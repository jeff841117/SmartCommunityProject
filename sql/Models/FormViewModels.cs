using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 這些 ViewModel 統一負責表單欄位與基本驗證，
    // 讓 Controller 不需要再手寫一大堆 if / else 檢查。
    public class LoginFormViewModel
    {
        [Required(ErrorMessage = "請輸入帳號。")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼。")]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AddAccountFormViewModel
    {
        [Required(ErrorMessage = "請輸入帳號名稱。")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼。")]
        public string Password { get; set; } = string.Empty;

        [Range(1, 200, ErrorMessage = "年齡需介於 1 到 200 之間。")]
        public double Age { get; set; }

        [EmailAddress(ErrorMessage = "請輸入正確的電子郵件格式。")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AddEquipmentFormViewModel
    {
        [Required(ErrorMessage = "請輸入設備名稱。")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇設備種類。")]
        public string EquipmentCategory { get; set; } = "場館";

        [Range(1, 100, ErrorMessage = "同時上限人數需介於 1 到 100。")]
        public byte MaxUsers { get; set; }

        [Range(1, 1440, ErrorMessage = "可使用時間需介於 1 到 1440 分鐘。")]
        public short AvailableTime { get; set; }

        [Required(ErrorMessage = "請輸入開放時間。")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "請輸入關閉時間。")]
        public TimeSpan CloseTime { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class UpdateEquipmentFormViewModel
    {
        [Required(ErrorMessage = "請提供設備編號。")]
        public byte Id { get; set; }

        [Required(ErrorMessage = "請輸入設備名稱。")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇設備種類。")]
        public string EquipmentCategory { get; set; } = "場館";

        // 舊資料中已有超過 100 人的設備，所以更新階段放寬到 byte 上限，
        // 避免只改設備種類時，被既有資料一起擋住。
        [Range(1, 255, ErrorMessage = "同時上限人數需介於 1 到 255。")]
        public byte MaxUsers { get; set; }

        // 舊資料中已有 0 分鐘紀錄，所以更新時保留相容性。
        [Range(0, 1440, ErrorMessage = "可使用時間需介於 0 到 1440 分鐘。")]
        public short AvailableTime { get; set; }

        [Required(ErrorMessage = "請輸入開放時間。")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "請輸入關閉時間。")]
        public TimeSpan CloseTime { get; set; }
    }

    public class DeleteEquipmentFormViewModel
    {
        [Required(ErrorMessage = "請提供設備編號。")]
        public byte Id { get; set; }
    }

    public class UpdateAccountFormViewModel
    {
        [Required(ErrorMessage = "請提供帳號編號。")]
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入密碼。")]
        public string Password { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "請輸入正確的電子郵件格式。")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
    }

    public class ForgotPasswordFormViewModel
    {
        [Required(ErrorMessage = "請輸入註冊時設定的電子郵件。")]
        [EmailAddress(ErrorMessage = "請輸入正確的電子郵件格式。")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入 6 碼驗證碼。")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "驗證碼需為 6 碼。")]
        public string VerificationCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入新密碼。")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "請再次輸入新密碼。")]
        [Compare(nameof(NewPassword), ErrorMessage = "兩次輸入的新密碼不一致。")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Step { get; set; } = "request";
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public string DebugCode { get; set; } = string.Empty;
    }

    // 管理者在後台調整未來預約時段時，先用這個模型接住輸入。
    public class AdminRescheduleReservationFormViewModel
    {
        [Required(ErrorMessage = "請提供預約編號。")]
        public int ReservationId { get; set; }

        [Required(ErrorMessage = "請選擇新的預約日期。")]
        public string ReservationDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇新的預約時間。")]
        public string SelectedSlotStartTime { get; set; } = string.Empty;

        public bool ConfirmQueueExpected { get; set; }
    }
}
