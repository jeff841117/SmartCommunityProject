using System.ComponentModel.DataAnnotations;

namespace 系統端.Models
{
    public class Package
    {
        public int ID { get; set; }
        public string PID { get; set; }
        [Required(ErrorMessage = "必填欄位")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "姓名字數錯誤")]
        public string Name { get; set; }
        [Required(ErrorMessage = "必填欄位")]
        [RegularExpression(@"^(09\d{8}|0\d{1,2}-\d{6,8 })$", ErrorMessage = "電話號碼格式錯誤,例如 09xxxxxxxx 或 0x-xxxxxx")]
        public string PhoneNumber { get; set; }
        public bool STA { get; set; }
        public DateTime Date { get; set; }
        public List<Package> Packages { get; set; }
    }
}
