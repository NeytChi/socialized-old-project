namespace Domain.Users
{
    public interface IUserRepository
    {
        void Create(User user);
        void Update(User user);
        void Delete(User user);
        User GetByEmail(string email);
        User GetByEmail(string email, bool deleted);
        User GetByEmail(string email, bool deleted, bool activate);
        User GetByUserTokenNotDeleted(string userToken);
        User GetByRecoveryToken(string recoveryToken, bool deleted);
        User GetByHash(string hash, bool deleted, bool activate);
    }
}
