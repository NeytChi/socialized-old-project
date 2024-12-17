namespace UseCases.Users.Commands
{
    public class CheckRecoveryCodeCommand
    {
        public string UserEmail { get; set; }
        public int RecoveryCode { get; set; }
    }
}
