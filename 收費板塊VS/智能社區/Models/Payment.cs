using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCommunity.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int BillID { get; set; }

        // 對應資料庫欄位 PayDate (datetime)
        [Required]
        [Column("PayDate")]
        [Display(Name = "付款日期")]
        public DateTime PaymentDate { get; set; }   // DB 已有預設 getdate()，程式可不給預設值

        // 對應資料庫欄位 PayAmount (decimal(9,2))
        [Required]
        [Column("PayAmount", TypeName = "decimal(9,2)")]
        [Display(Name = "繳款金額")]
        public decimal Amount { get; set; }

        // 對應資料庫欄位 PayMethod (nvarchar(40))
        [MaxLength(40)]
        [Column("PayMethod")]
        [Display(Name = "繳款方式")]
        public string? PaymentMethod { get; set; }

        // 導覽屬性
        public Bill? Bill { get; set; }
    }
}