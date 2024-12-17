namespace UseCases.Users.Commands
{
    public class ChangeOldPasswordCommand
    {
        public string UserToken { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
