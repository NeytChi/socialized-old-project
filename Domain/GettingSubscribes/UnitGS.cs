namespace Domain.GettingSubscribes
{
    public partial class UnitGS : BaseEntity
    {
        public long DataId { get; set; }
        public long UserPk { get; set; }
        public bool UserIsPrivate { get; set; }
        public string Username { get; set; }
        public string CommentPk { get; set; }
        public long CreatedAt { get; set; }    
        public bool UnitHandled { get; set; }    
        public long? HandledAt { get; set; }    
        public bool HandleAgain { get; set; }    
        public virtual TaskData Data { get; set; }
        public virtual ICollection<MediaGS> Medias { get; set; }
    }
} 