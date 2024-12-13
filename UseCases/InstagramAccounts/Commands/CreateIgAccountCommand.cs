namespace UseCases.InstagramAccounts.Commands
{
    public class CreateIgAccountCommand
    {
        public string UserToken { get; set; }
        public string InstagramUsername { get; set; }
        public string InstagramPassword { get; set; }
    }
}
