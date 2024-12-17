namespace UseCases.Users.Commands
{
    public class ChangePasswordCommand
    {
        public string RecoveryToken { get; set; }
        public string UserPassword { get; set; } 
        public string UserConfirmPassword { get; set; } 
    }
}
