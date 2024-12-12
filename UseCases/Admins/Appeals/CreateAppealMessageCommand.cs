namespace UseCases.Admins.Appeals
{
    public class CreateAppealMessageCommand
    {
        public long AppealId { get; set; }
        public string Message { get; set; }
    }
}
