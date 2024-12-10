namespace UseCases.Admins.Appeals
{
    public class CreateAppealCommand
    {
        public string UserToken { get; set; }
        public string Subject { get; set; }
    }
}
