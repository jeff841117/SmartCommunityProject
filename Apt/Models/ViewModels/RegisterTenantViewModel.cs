
using System.ComponentModel.DataAnnotations;

namespace Apt.Models.ViewModels
{
    public class RegisterTenantViewModel
    {
        [Required(ErrorMessage = "請輸入用戶名")]
        [StringLength(50, ErrorMessage = "用戶名長度不能超過50個字元")]
        public string Username { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須為6到100個字元")]
        [Compare("ConfirmPassword", ErrorMessage = "密碼與確認密碼不匹配")]
        public string Password { get; set; }
         
        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入全名")]
        [StringLength(100, ErrorMessage = "全名長度不能超過100個字元")]
        public string FullName { get; set; }

        [StringLength(200, ErrorMessage = "地址長度不能超過200個字元")]
        public string Address { get; set; }

        [StringLength(20, ErrorMessage = "電話長度不能超過20個字元")]
        [Phone(ErrorMessage = "電話格式不正確")]
        public string Phone { get; set; }
    }
}