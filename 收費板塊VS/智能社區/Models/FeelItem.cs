namespace SmartCommunity.Models
{
    public class FeeItem
    {
        public int FeeItemID { get; set; }
        public string ItemName { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
    }
}