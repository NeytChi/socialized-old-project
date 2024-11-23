namespace Domain.GettingSubscribes
{
    /// <summary>
    /// This class provide data of handled Instagram medias.
    /// For example, task with type -> Liking.
    /// </summary>
    public partial class MediaGS
    {
        public long mediaId { get; set; }
        public long unitId { get; set; }
        public string mediaPk { get; set; }
        public int mediaQueue { get; set; }
        public bool mediaHandled { get; set; }    
        public long? handledAt { get; set; }    
        public virtual UnitGS unit { get; set; }
    }
} 