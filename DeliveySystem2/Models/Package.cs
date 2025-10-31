using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveySystem2.Models
{
    // 共用包裹類別
    public class Package
    {
        public int ID { get; set; }
        public string PID { get; set; }

        [Required(ErrorMessage = "必填欄位")]
        [StringLength(5, MinimumLength = 2, ErrorMessage = "姓名字數錯誤")]
        public string Name { get; set; }

        [Required(ErrorMessage = "必填欄位")]
        [RegularExpression(@"^(09\d{8}|0\d{1,2}-\d{6,8})$",
            ErrorMessage = "電話號碼格式錯誤, 例如 09xxxxxxxx 或 0x-xxxxxx")]
        public string PhoneNumber { get; set; }

        public bool STA { get; set; }

        // 對應前端顯示文字
        public string STA_Text => STA ? "已領取" : "未領取";

        public DateTime Date { get; set; }

        // 可放子包裹列表（若有多層結構）
        public List<Package> Packages { get; set; } = new List<Package>();
    }

    // 詳細寄件資料
    public class Details
    {
        public int PackageID { get; set; }
        public string SenderName { get; set; }
        public string SenderPhn { get; set; }
        public string SenderAddress { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhn { get; set; }
        public string RecipientAddress { get; set; }
        public DateTime SendDate { get; set; }
        public string DeliveryStatus { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}