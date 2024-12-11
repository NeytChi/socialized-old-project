namespace UseCases.Admins
{
    public interface IAdminEmailManager
    {
        void SetupPassword(string tokenForStart, string email);
        void RecoveryPassword(int code, string email);
    }
}
