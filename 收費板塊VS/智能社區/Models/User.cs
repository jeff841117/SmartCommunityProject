namespace SmartCommunity.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string UserName { get; set; } = "";
        public string RoomNumber { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? AspNetUserId { get; set; }   // ★ 關聯到 AspNetUsers.Id
        public ICollection<Bill>? Bills { get; set; }
    }
}