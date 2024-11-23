using Domain.Admins;
using Domain.Users;
using Serilog;

namespace UseCases.Admins
{
    public class EmailFollowerManager
    {
        private ILogger Logger;
        private IEmailFollowerRepository Repository;

        public EmailFollowerManager(IEmailFollowerRepository repository, ILogger logger)
        {
            Logger = logger;
            Repository = repository;
        }
        public void UpdateExistFollower(string userEmail, int userId)
        {
            var follower = Repository.GetByEmail(userEmail);
            if (follower != null)
            {
                follower.userId = userId;
                Repository.Update(follower);
                Logger.Information("Updare exist followers, set user id, id -> " + follower.followerId);
            }
            Logger.Information("User doesn't have following on lending");
        }
        public void BindWithFollower(string email, int userId)
        {
            var follower = Repository.GetByEmail(email);
            if (follower != null)
            {
                follower.userId = userId;
                Repository.Update(follower);
            }
        }
    }
}
