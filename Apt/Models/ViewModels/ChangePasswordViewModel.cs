using System.ComponentModel.DataAnnotations;

namespace Apt.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string Email { get; set; }
        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        [Display(Name = "新密碼")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須為6到100個字元")]
        [Compare("ConfirmNewPassword", ErrorMessage = "密碼與確認密碼不匹配")]
        public string   NewPassword { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        [Display(Name ="確認新密碼")]
        public string ConfirmNewPassword { get; set; }
    }
}
