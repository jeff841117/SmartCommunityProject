using System.ComponentModel.DataAnnotations;

namespace Apt.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "請輸入用戶名")]
        public string Username { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "請選擇角色")]
        public string Role { get; set; } // "Tenant" 或 "Admin"
        [Required(ErrorMessage = "請輸入電子信箱")]
        [EmailAddress]
        public string Email {  get; set; }
        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}