namespace Domain.Statistics
{
    public partial class DayStatistics
    {
        public long statisticsId { get; set; }
        public long accountId { get; set; }
        public int followerCount { get; set; }
        public int emailContacts { get; set; }
        public long profileViews { get; set; }
        public int getDirectionsClicks { get; set; }
        public int phoneCallClicks { get; set; }
        public int textMessageClicks { get; set; }
        public int websiteClicks { get; set; }
        public long impressions { get; set; }
        public long reach { get; set; }
        public DateTime endTime { get; set; }
        public virtual BusinessAccount Account { get; set; }
    }
}