namespace SmartCommunity.Models
{
    public class CreateBillVm
    {
        // 下拉選單值
        public int UserId { get; set; }
        public int FeeItemId { get; set; }

        // 顯示用
        public string? UserLabel { get; set; }      // 王小明 (A101)
        public string? FeeItemName { get; set; }    // 管理費/水費/電費

        // 計價輸入
        public decimal? Usage { get; set; }         // 可選：輸入度數（用於水/電）
        public decimal? Amount { get; set; }        // 可選：直接輸入金額（不用度數時）

        // 後台頁面選單資料
        public List<(int Id, string Label)> Users { get; set; } = new();
        public List<(int Id, string Name, decimal UnitPrice, string Unit)> FeeItems { get; set; } = new();

        // 送出後訊息
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}