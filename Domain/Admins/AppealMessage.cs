namespace Domain.Admins
{
    public partial class AppealMessage : BaseEntity
    {
        public AppealMessage()
        {
            Files = new HashSet<AppealFile>();
            AppealMessageReplies = new HashSet<AppealMessageReply>();
        }
        public long AppealId { get; set; }
        public string Message { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public virtual Appeal Appeal { get; set; }
        public virtual Admin Admin { get; set; }
        public virtual ICollection<AppealFile> Files { get; set; }
        public virtual ICollection<AppealMessageReply> AppealMessageReplies { get; set; }
    }
}