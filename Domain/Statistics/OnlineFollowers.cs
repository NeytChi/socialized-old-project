using Domain.InstagramAccounts;

namespace Domain.Statistics
{
    public partial class OnlineFollowers : BaseEntity
    {
        public long AccountId { get; set; }
        public long Value { get; set; }
        public DateTime EndTime { get; set; }
        public virtual BusinessAccount Account { get; set; }
    }
}