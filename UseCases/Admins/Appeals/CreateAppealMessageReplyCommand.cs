namespace UseCases.Admins.Appeals
{
    public class CreateAppealMessageReplyCommand
    {
        public long AppealMessageId { get; set; }
        public string Reply { get; set; }
    }
}
