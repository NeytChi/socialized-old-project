namespace Domain.Statistics
{
    public partial class PostStatistics
    {
        public long postId { get; set; }
        public long accountId { get; set; }
        public long likeCount { get; set; }
        public string IGMediaId { get; set; }
        public string postUrl { get; set; }
        public int commentsCount { get; set; }
        public string mediaType { get; set; }
        public long engagement { get; set; }
        public long impressions { get; set; }
        public long reach { get; set; }
        public long saved { get; set; }
        public long videoViews { get; set; }
        public DateTime timestamp { get; set; }
        public virtual BusinessAccount Account { get; set; }
        public virtual ICollection<CommentStatistics> Comments { get; set; }
    }
}