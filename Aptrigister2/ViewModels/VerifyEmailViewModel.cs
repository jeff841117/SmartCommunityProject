using System.ComponentModel.DataAnnotations;

namespace Aptrigister2.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress]
        public string Email { get; set; }
    }
}
