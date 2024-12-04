namespace Domain.Statistics
{
    public partial class StoryStatistics
    {
        public long storyId { get; set; }
        public long accountId { get; set; }
        public string mediaId { get; set; }
        public string storyUrl { get; set; }
        public string storyType { get; set; }
        public int replies { get; set; }
        public bool exists { get; set; }
        public long impressions { get; set; }
        public long reach { get; set; }
        public DateTime timestamp { get; set; }
        public virtual BusinessAccount Account { get; set; }
    }
}