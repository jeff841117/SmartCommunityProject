using System.ComponentModel.DataAnnotations;

namespace sql.Models
{
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
        [Required(ErrorMessage = "請輸入帳號。")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入密碼。")]
        [MinLength(8, ErrorMessage = "密碼長度至少需要 8 個字元。")]
        public string Password { get; set; } = string.Empty;

        [Range(1, 200, ErrorMessage = "年齡需介於 1 到 200 之間。")]
        public double Age { get; set; }

        [EmailAddress(ErrorMessage = "請輸入正確的電子郵箱格式。")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AddEquipmentFormViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "請輸入設備名稱。")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇設備種類。")]
        public string EquipmentCategory { get; set; } = "場館";

        [Range(1, 100, ErrorMessage = "同時上限人數需介於 1 到 100 之間。")]
        public byte MaxUsers { get; set; }

        [Range(1, 1440, ErrorMessage = "可使用時間需介於 1 到 1440 分鐘之間。")]
        public short AvailableTime { get; set; }

        [Required(ErrorMessage = "請輸入開放時間。")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "請輸入關閉時間。")]
        public TimeSpan CloseTime { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (OpenTime >= CloseTime)
            {
                yield return new ValidationResult("開放時間必須早於關閉時間。", [nameof(OpenTime), nameof(CloseTime)]);
            }
        }
    }

    public class UpdateEquipmentFormViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "請提供設備編號。")]
        public byte Id { get; set; }

        [Required(ErrorMessage = "請輸入設備名稱。")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請選擇設備種類。")]
        public string EquipmentCategory { get; set; } = "場館";

        [Range(1, 255, ErrorMessage = "同時上限人數需介於 1 到 255 之間。")]
        public byte MaxUsers { get; set; }

        [Range(1, 1440, ErrorMessage = "可使用時間需介於 1 到 1440 分鐘之間。")]
        public short AvailableTime { get; set; }

        [Required(ErrorMessage = "請輸入開放時間。")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "請輸入關閉時間。")]
        public TimeSpan CloseTime { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (OpenTime >= CloseTime)
            {
                yield return new ValidationResult("開放時間必須早於關閉時間。", [nameof(OpenTime), nameof(CloseTime)]);
            }
        }
    }

    public class DeleteEquipmentFormViewModel
    {
        [Required(ErrorMessage = "請提供設備編號。")]
        public byte Id { get; set; }
    }

    public class UpdateAccountFormViewModel
    {
        [Required(ErrorMessage = "請提供要更新的帳號編號。")]
        public int Id { get; set; }

        [MinLength(8, ErrorMessage = "若要修改密碼，長度至少需要 8 個字元。")]
        public string Password { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "請輸入正確的電子郵箱格式。")]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
    }

    public class ForgotPasswordFormViewModel
    {
        [Required(ErrorMessage = "請輸入註冊時使用的電子郵箱。")]
        [EmailAddress(ErrorMessage = "請輸入正確的電子郵箱格式。")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入 6 碼驗證碼。")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "驗證碼必須為 6 碼。")]
        public string VerificationCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入新密碼。")]
        [MinLength(8, ErrorMessage = "新密碼長度至少需要 8 個字元。")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "請再次輸入新密碼。")]
        [Compare(nameof(NewPassword), ErrorMessage = "確認密碼與新密碼不一致。")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Step { get; set; } = "request";
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        public string DebugCode { get; set; } = string.Empty;
    }

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
