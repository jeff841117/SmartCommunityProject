using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Delivery_user.Models
{
    public class Package
    {
        public int ID { get; set; }
        public string PID { get; set; }
        public string Name{ get; set; }
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
        public List<Package> Packages { get; set; }
    }
    public class Details
    {
        public int PackageID { get; set; }
        public string SenderName { get; set; }
        public string SenderPhn { get; set; }
        public string SenderAddress { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhn { get; set; }
        public string RecipientAddress { get; set; }
        public DateTime SendDate { get; set; }
        public string DeliveryStatus { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set;}
    }
}
