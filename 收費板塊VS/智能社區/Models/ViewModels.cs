namespace SmartCommunity.Models
{
    public class QueryRoomVm
    {
        public string RoomInput { get; set; } = ""; // 可輸入 A101 或 101
    }

    public class PayRequestVm
    {
        public int UserId { get; set; }
        public int BillId { get; set; }
        public decimal PayAmount { get; set; }
        public string PayMethod { get; set; } = ""; // 現金 / ATM / LINE Pay
    }
}
