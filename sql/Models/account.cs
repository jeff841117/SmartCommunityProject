using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sql.Models
{
    public class account
    {
        public int id { get; set; }
        public string userName { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public double age { get; set; }
        public string role { get; set; } = "user"; // 默认值为普通用户
        public string email { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
    }

}
