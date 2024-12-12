namespace UseCases.Appeals.Messages.Commands
{
    public class UpdateAppealMessageCommand
    {
        public long MessageId { get; set; }
        public string Message { get; set; }
    }
}
