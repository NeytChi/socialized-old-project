using Domain.InstagramAccounts;

namespace Domain.Statistics
{
    public partial class PostStatistics : BaseEntity
    {
        public long AccountId { get; set; }
        public long LikeCount { get; set; }
        public string IGMediaId { get; set; }
        public string Url { get; set; }
        public int CommentsCount { get; set; }
        public string MediaType { get; set; }
        public long Engagement { get; set; }
        public long Impressions { get; set; }
        public long Reach { get; set; }
        public long Saved { get; set; }
        public long VideoViews { get; set; }
        public DateTime Timestamp { get; set; }
        public virtual BusinessAccount Account { get; set; }
        public virtual ICollection<CommentStatistics> Comments { get; set; }
    }
}