using System;

namespace SmartCommunity.Models.ViewModels
{
    public class PaymentRowVm
    {
        public string RoomNumber { get; set; } = "";
        public string UserName { get; set; } = "";
        public string ItemName { get; set; } = "";
        public decimal PayAmount { get; set; }
        public DateTime PayDate { get; set; }
        public string PayMethod { get; set; } = "";
    }
}