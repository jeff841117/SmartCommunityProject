namespace SmartCommunity.Models
{
    public class Bill
    {
        public int BillID { get; set; }
        public int UserID { get; set; }
        public int FeeItemID { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "未繳";

        public User? User { get; set; }
        public FeeItem? FeeItem { get; set; }
    }
}