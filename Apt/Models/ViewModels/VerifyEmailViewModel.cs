using System.ComponentModel.DataAnnotations;

namespace Apt.Models.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "電子郵件格式不正確")]
        public string Email { get; set; }
    }
}
