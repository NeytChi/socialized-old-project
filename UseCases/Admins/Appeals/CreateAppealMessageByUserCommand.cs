namespace UseCases.Admins.Appeals
{
    public class CreateAppealMessageByUserCommand : CreateAppealMessageCommand
    {
        public string UserToken { get; set; }
    }
}
