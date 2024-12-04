using Domain.Statistics;

namespace Domain.InstagramAccounts
{
    public partial class BusinessAccount : BaseEntity
    {
        public long AccountId { get; set; }
        public string AccessToken { get; set; }
        public string ProfilePicture { get; set; }
        public string AccountUsername { get; set; }
        public string LongLiveAccessToken { get; set; }
        public string FacebookId { get; set; }
        public string BusinessAccountId { get; set; }
        public long FollowersCount { get; set; }
        public int MediaCount { get; set; }
        public DateTime LongTokenExpiresIn { get; set; }
        public DateTime TokenCreated { get; set; }
        public bool Received { get; set; }
        public bool StartProcess { get; set; }
        public DateTime StartedProcess { get; set; }
        public virtual IGAccount Account { get; set; }
        public virtual ICollection<DayStatistics> Statistics { get; set; }
        public virtual ICollection<OnlineFollowers> OnlineFollowers { get; set; }
        public virtual ICollection<PostStatistics> Posts { get; set; }
        public virtual ICollection<StoryStatistics> Stories { get; set; }

    }
}