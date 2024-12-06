namespace UseCases.AutoPosts.Commands
{
    public class ChangePasswordCommand
    {
        public int RecoveryCode { get; set; }
        public string Password { get; set; }
    }
}
