using System.ComponentModel.DataAnnotations;

namespace DeliveySystem2.Models
{
    public class LoginView
    {
        [Required(ErrorMessage = "帳號為必填")]
        [RegularExpression(@"^(?=.{1,10}$)(?=.*\d)(?=.*[A-Za-z])[A-Z][A-Za-z0-9]*$",
            ErrorMessage = "帳號格式錯誤：第一字需大寫、英數混合且長度不超過10字元")]
        public string account { get; set; }
        [Required(ErrorMessage = "密碼為必填")]
        [RegularExpression(@"^(?=.{1,10}$)(?=.*\d)(?=.*[A-Za-z])[A-Z][A-Za-z0-9]*$",
            ErrorMessage = "密碼格式錯誤：第一字需大寫、英數混合且長度不超過10字元")]
        public string password { get; set; }
    }
}
