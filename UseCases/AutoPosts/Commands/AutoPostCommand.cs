namespace UseCases.AutoPosts.Commands
{
    public class AutoPostCommand
    {
        public long AccountId { get; set; }
        public bool AutoPostType { get; set; }
        public bool AutoDelete { get; set; }
        public bool Stopped { get; set; }
        public DateTime ExecuteAt { get; set; }
        public DateTime DeleteAfter { get; set; }
        public string Location { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
        public long CategoryId { get; set; }
        public int TimeZone { get; set; }
    }
}
