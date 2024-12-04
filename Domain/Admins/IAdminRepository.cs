namespace Domain.Admins
{
    public interface IAdminRepository
    {
        void Create(Admin admin);
        Admin Update(Admin admin);
        Admin GetByAdminId(long id);
        Admin GetByRecoveryCode(int recoveryCode);
        Admin GetByEmail(string email);
        Admin GetByPasswordToken(string email);
        Admin[] GetActiveAdmins(long adminId, int since, int count);
        dynamic GetUser(int since, int count);
        dynamic GetFollowers(int since, int count);
    }
}
