using System.ComponentModel.DataAnnotations;
using Delivery_user.Models;

namespace Delivery_user.Models
{
    public class DeliveryViewModel
    {
        public int PackageID { get; set; }
        [Required(ErrorMessage = "必填欄位")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "姓名字數錯誤")]
        public string SenderName { get; set; }
        [Required(ErrorMessage = "必填欄位")]
        [RegularExpression(@"^(09\d{8}|0\d{1,2}-\d{6,8 })$", ErrorMessage ="電話號碼格式錯誤,例如 09xxxxxxxx 或 0x-xxxxxx")]
        public string SenderPhn { get; set; }
        [Required(ErrorMessage = "地址為必填欄位")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "地址長度必須介於 5 到 100 個字元")]
        [RegularExpression(@"^[\u4e00-\u9fa5a-zA-Z0-9\s\-#之巷弄路街號樓鄰鄉鎮市區村里]+$",
        ErrorMessage = "地址格式不正確，只能包含中文、英文、數字與一般地址符號")]
        public string SenderAddress { get; set; }
        [Required(ErrorMessage = "必填欄位")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "姓名字數錯誤")]
        public string RecipientName { get; set; }
        [Required(ErrorMessage = "必填欄位")]
        [RegularExpression(@"^(09\d{8}|0\d{1,2}-\d{6,8 })$", ErrorMessage = "電話號碼格式錯誤,例如 09xxxxxxxx 或 0x-xxxxxx")]
        public string RecipientPhn { get; set; }
        [Required(ErrorMessage = "地址為必填欄位")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "地址長度必須介於 5 到 100 個字元")]
        [RegularExpression(@"^[\u4e00-\u9fa5a-zA-Z0-9\s\-#之巷弄路街號樓鄰鄉鎮市區村里]+$",
        ErrorMessage = "地址格式不正確，只能包含中文、英文、數字與一般地址符號")]
        public string RecipientAddress { get; set; }
        [Required(ErrorMessage ="請選擇日期")]
        public string SendDateString { get; set; }
        [DateCheck(ErrorMessage="日期不能小於今日")]
       public DateTime SendDate { get; set; }
        public string DeliveryStatus { get; set; }
        public DateTime CreatDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public List<Details> DetailList { get; set; }
    }
}
