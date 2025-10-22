
namespace SmartCommunity.Models.ViewModels
{
    public class PayRequestVm
    {
        public int UserId { get; set; }
        public int BillId { get; set; }
        public string PayMethod { get; set; } = "";
        public decimal PayAmount { get; set; }

        // 用於付款完成後 Redirect 回同一房號的帳單頁
        public string? RoomInput { get; set; }
    }
}