namespace Domain.GettingSubscribes
{
    public partial class HistoryGS : BaseEntity
    {
        public long TaskId { get; set; }
        public string Url  { get; set; }
        public virtual TaskGS Task { get; set; }
    }
} 