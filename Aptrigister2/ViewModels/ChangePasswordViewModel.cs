using System.ComponentModel.DataAnnotations;

namespace Aptrigister2.ViewModels
{
    public class ChangePasswordViewModel
    {
        
            [Required(ErrorMessage = "請輸入電子郵件")]
            [EmailAddress]
            public string Email { get; set; }

            [Required(ErrorMessage = "請輸入密碼")]
            [DataType(DataType.Password)]
            [Display(Name = "新密碼")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "The {0}must be at {2}and at max {1}characters long.")]
            [Compare("ConfirmNewPassword", ErrorMessage = "密碼與確認密碼不匹配")]
            public string NewPassword { get; set; }

            [Required(ErrorMessage = "請輸入密碼")]
            [DataType(DataType.Password)]
            [Display(Name = "確認新密碼")]
            public string ConfirmNewPassword { get; set; }
        
    }
}
