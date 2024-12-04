namespace Domain.Statistics
{
    public partial class OnlineFollowers
    {
        public long followersId { get; set; }
        public long accountId { get; set; }
        public long value { get; set; }
        public DateTime endTime { get; set; }
        public virtual BusinessAccount Account { get; set; }
    }
}