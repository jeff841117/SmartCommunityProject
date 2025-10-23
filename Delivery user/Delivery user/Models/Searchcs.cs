using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Delivery_user.Models
{
    public class SearchViewModel
    {
        public int id { get; set; }
        public string PID { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public bool STA { get; set; }
        public string STA_Text
        {
            get
            {
                return STA ? "已領取" : "未領取";
            }
        }
        public DateTime Date { get; set; }
        public List<Package> Packages { get; set; }   // 存放包裹資料
        public bool HassSearched { get; set; }
        public string SearchKeyword { get; set; }     // 頁面搜尋或控制用
    }
}