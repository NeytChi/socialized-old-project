namespace Domain.InstagramAccounts
{
    public partial class AccountProfile : BaseEntity
    {
        public long AccountId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Username { get; set; }
        public long PostsCount { get; set; }
        public long FollowingCount { get; set; }
        public long SubscribersCount { get; set; }
        public string AvatarUrl { get; set; }
        public long SubscribersGS { get; set; }
        public long SubscribersTodayGS { get; set; }
        public long ConversionGS { get; set; }
        public virtual IGAccount Account { get; set; }
    }
}