namespace Domain.GettingSubscribes
{
    public partial class MediaGS : BaseEntity
    {
        public long UnitId { get; set; }
        public string Pk { get; set; }
        public int Queue { get; set; }
        public bool Handled { get; set; }    
        public long? HandledAt { get; set; }    
        public virtual UnitGS Unit { get; set; }
    }
} 