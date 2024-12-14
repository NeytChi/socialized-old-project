using Core;
using Domain.Users;
using Serilog;
using UseCases.Exceptions;
using UseCases.Users.Commands;

namespace UseCases.Users
{
    public interface IUserLoginManager
    {
        User Login(LoginUserCommand command);
        void LogOut(string userToken);
    }
    public class UserLoginManager : BaseManager, IUserLoginManager
    {
        private IUserRepository UserRepository;
        private ProfileCondition ProfileCondition = new ProfileCondition();

        public UserLoginManager(ILogger logger,
            IUserRepository userRepository) : base(logger) 
        {
            UserRepository = userRepository;
        }
        public User Login(LoginUserCommand command)
        {
            var user = UserRepository.GetByEmail(command.Email);
            if (user == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по email для логіну.");
            }
            if (!ProfileCondition.VerifyHashedPassword(user.Password, command.Password))
            {
                throw new ValidationException("Пароль користувача не співпадає з паролем на сервері.");
            }
            user.LastLoginAt = DateTime.UtcNow;
            UserRepository.Update(user);
            Logger.Information($"Користувач був залогінен, id={user.Id}.");
            return user;
        }
        public void LogOut(string userToken)
        {
            var user = UserRepository.GetByUserTokenNotDeleted(userToken);

            if (user == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по токен для активації аккаунту.");
            }
            user.TokenForUse = ProfileCondition.CreateHash(40);
            UserRepository.Update(user);
            Logger.Information($"Користувач вийшов з сервісу, id={user.Id}.");
        }
    }
}
