namespace Domain.Admins
{
    public class AppealMessageReply : BaseEntity
    {
        public long AppealMessageId { get; set; }
        public string Reply {  get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual AppealMessage Message { get; set; }
    }
}
