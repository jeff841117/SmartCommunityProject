namespace SmartCommunity.Models.ViewModels
{
    public class BillRowVm
    {
        public int BillID { get; set; }
        public string RoomNumber { get; set; } = "";
        public string UserName { get; set; } = "";
        public string ItemName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
    }
}
