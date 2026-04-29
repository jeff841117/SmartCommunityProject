using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
    // 這份檔案集中管理「畫面表單輸入模型」。
    // 目的不是取代資料表模型，而是把每個頁面真正需要的輸入欄位定清楚。
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

        [Range(1, 200, ErrorMessage = "請輸入正確年齡")]
        public double Age { get; set; }

        [EmailAddress(ErrorMessage = "請輸入正確的電子郵件")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AddEquipmentFormViewModel
    {
        [Required(ErrorMessage = "請輸入設備名稱")]
        public string EquipmentName { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "請輸入正確的最大使用人數")]
        public byte MaxUsers { get; set; }

        [Range(1, 1440, ErrorMessage = "請輸入正確的可使用分鐘數")]
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

        [Range(1, 100, ErrorMessage = "請輸入正確的最大使用人數")]
        public byte MaxUsers { get; set; }

        [Range(1, 1440, ErrorMessage = "請輸入正確的可使用分鐘數")]
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
        [Required(ErrorMessage = "缺少帳號編號")]
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        public string Password { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "請輸入正確的電子郵件")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
    }

    public class ForgotPasswordFormViewModel
    {
        [Required(ErrorMessage = "請輸入註冊電子郵件")]
        [EmailAddress(ErrorMessage = "請輸入正確的電子郵件")]
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
}
