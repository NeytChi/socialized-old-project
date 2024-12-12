namespace UseCases.Appeals.Replies.Commands
{
    public class UpdateAppealMessageReplyCommand
    {
        public long ReplyId { get; set; }
        public string Reply { get; set; }
    }
}
