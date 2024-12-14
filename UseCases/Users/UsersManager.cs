using Core;
using Serilog;
using System.Web;
using Domain.Users;
using UseCases.Packages;
using UseCases.Users.Commands;
using UseCases.Exceptions;
using Domain.Packages;

namespace UseCases.Users
{
    public interface IUsersManager
    {
        void Create(CreateUserCommand command);
        void RegistrationEmail(string userEmail, string culture);
        void Activate(string hash);
        void Delete(string userToken);
    }
    public class UsersManager : BaseManager, IUsersManager
    {
        private IUserRepository UserRepository;
        
        public ProfileCondition ProfileCondition = new ProfileCondition();
        public PackageManager PackageCondition;
        private IEmailMessager EmailMessanger;

        public UsersManager(ILogger logger,
            IUserRepository userRepository,
            IEmailMessager emailMessager) : base(logger) 
        {
            UserRepository = userRepository;
            EmailMessanger = emailMessager;
        }
        public void Create(CreateUserCommand command)
        {
            var user = UserRepository.GetByEmail(command.Email);
            if (user != null)
            {
                if (user.IsDeleted)
                {
                    user.IsDeleted = false;
                    user.TokenForUse = Guid.NewGuid().ToString();
                    UserRepository.Update(user);
                    Logger.Information($"Був востановлен видалений аккаунт, id={user.Id}.");
                }
                throw new NotFoundException("Користувач з таким email-адресом вже існує.");
            }
            user = new User
            {
                Email = command.Email,
                FirstName = HttpUtility.UrlDecode(command.FirstName),
                LastName = HttpUtility.UrlDecode(command.LastName),
                Password = ProfileCondition.HashPassword(command.Password),
                HashForActivate = ProfileCondition.CreateHash(100),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                TokenForUse = ProfileCondition.CreateHash(40),
                profile = new Profile
                {
                    CountryName = HttpUtility.UrlDecode(command.CountryName),
                    TimeZone = command.TimeZone
                },
                access = new ServiceAccess()
            };            
            UserRepository.Create(user);
            PackageCondition.CreateDefaultServiceAccess(user.Id);
            EmailMessanger.SendConfirmEmail(user.Email, command.Culture, user.HashForActivate);
            Logger.Information($"Новий користувач був створений, id={user.Id}.");
        }
        public void RegistrationEmail(string userEmail, string culture)
        {
            var user = UserRepository.GetByEmail(userEmail);
            if (user == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по email для активації аккаунту.");
            }
            EmailMessanger.SendConfirmEmail(user.Email, culture, user.HashForActivate);
            Logger.Information($"Відправлен лист на підтверждення реєстрації користувача, id={user.Id}.");                
        }
        public void Activate(string hash)
        {
            var user = UserRepository.GetByHash(hash, false, false);
            if (user == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по хешу для активації аккаунту.");
            }
            user.Activate = true;
            UserRepository.Update(user);
            Logger.Information($"Користувач був активований завдяки хешу з пошти, id={user.Id}.");
        }
        public void Delete(string userToken)
        {
            var user = UserRepository.GetByUserTokenNotDeleted(userToken);
            if (userToken == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по його токену для видалення аккаунту.");                
            }
            user.IsDeleted = true;
            user.TokenForUse = null;
            UserRepository.Update(user);
            Logger.Information($"Користувач був видалений, id={user.Id}.");
        }
    }
}