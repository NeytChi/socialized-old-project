namespace Domain.Statistics
{
    public partial class CommentStatistics : BaseEntity
    {
        public long MediaId { get; set; }
        public string InstagramId { get; set; }
        public DateTime Timestamp { get; set; }
        public virtual PostStatistics Post { get; set; }       
    }
}