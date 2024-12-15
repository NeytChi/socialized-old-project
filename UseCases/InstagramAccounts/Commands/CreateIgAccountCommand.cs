namespace UseCases.InstagramAccounts.Commands
{
    public class CreateIgAccountCommand : IgAccountRequirements
    {
        public string UserToken { get; set; }
    }
}
