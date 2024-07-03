namespace Models.GettingSubscribes
{
    /// <summary>
    /// History of work done on the tasks of getting subscribers.
    /// </summary>
    public partial class HistoryGS
    {
        public long historyId { get; set; }
        public long taskId { get; set; }
        public long createdAt { get; set; }
        public string historyUrl  { get; set; }
        public virtual TaskGS task { get; set; }
    }
} 