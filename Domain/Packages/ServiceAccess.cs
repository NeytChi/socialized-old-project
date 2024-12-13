using Domain.Users;

namespace Domain.Packages
{
    public partial class ServiceAccess : BaseEntity
    {
        public long UserId { get; set; }
        public bool Available { get; set; }
        public long Type { get; set; }
        public bool Paid { get; set; }
        public DateTime PaidAt { get; set; }
        public DateTime DisableAt { get; set; }
        public virtual User User { get; set; }
    }
}