using System;

using Models.Common;

namespace Models.SessionComponents
{
    public partial class ServiceAccess
    {
        public int accessId { get; set; }
        public int userId { get; set; }
        
        public bool available { get; set; }
        
        public int packageType { get; set; }
        public bool paid { get; set; }
        public DateTime paidAt { get; set; }
        public DateTime disableAt { get; set; }
        public virtual User User { get; set; }
    }
}