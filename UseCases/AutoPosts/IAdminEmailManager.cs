namespace UseCases.AutoPosts
{
    public interface IAdminEmailManager
    {
        void SetupPassword(string tokenForStart, string email);
        void RecoveryPassword(int code, string email);
    }
}
