using Core;
using Domain.Users;
using Serilog;
using UseCases.Exceptions;

namespace UseCases.Users
{
    public interface IUserPasswordRecoveryManager
    {
        void RecoveryPassword(string userEmail, string culture);
        string CheckRecoveryCode(string userEmail, int recoveryCode);
        void ChangePassword(string recoveryToken, string userPassword, string userConfirmPassword);
        void ChangeOldPassword(string userToken, string oldPassword, string newPassword);
    }
    public class UserPasswordRecoveryManager : BaseManager, IUserPasswordRecoveryManager
    {
        private IUserRepository UserRepository;
        private ProfileCondition ProfileCondition = new ProfileCondition();
        private IEmailMessager EmailMessager;

        public UserPasswordRecoveryManager(ILogger logger,
            IUserRepository userRepository,
            IEmailMessager emailMessager) : base (logger) 
        {
            UserRepository = userRepository;
            EmailMessager = emailMessager;
        }
        public void RecoveryPassword(string userEmail, string culture)
        {
            var user = UserRepository.GetByEmail(userEmail, false, true);

            if (user == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по email для активації аккаунту.");
            }
            user.RecoveryCode = ProfileCondition.CreateCode(6);
            UserRepository.Update(user);
            EmailMessager.SendRecoveryEmail(user.Email, culture, (int)user.RecoveryCode);
            Logger.Information($"Пароль був востановлений для користувача, id={user.Id}.");
        }
        public string CheckRecoveryCode(string userEmail, int recoveryCode)
        {
            var user = UserRepository.GetByEmail(userEmail);
            if (user == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по email для перевірки коду востановлення паролю.");
            }
            if (user.RecoveryCode != recoveryCode)
            {
                throw new ValidationException("Код востановлення паролю не вірний.");
            }
            user.RecoveryToken = ProfileCondition.CreateHash(40);
            user.RecoveryCode = -1;
            UserRepository.Update(user);
            Logger.Information($"Перевірен був код востановлення паролю користувача, id={user.Id}.");
            return user.RecoveryToken;
        }
        public void ChangePassword(string recoveryToken, string userPassword, string userConfirmPassword)
        {            
            var user = UserRepository.GetByRecoveryToken(recoveryToken, false);
            if (user == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по токену востановлення для зміни паролю.");
            }
            if (!userPassword.Equals(userConfirmPassword))
            {
                throw new ValidationException("Паролі не співпадають одне з одним.");
            }
            user.Password = ProfileCondition.HashPassword(userPassword);
            user.RecoveryToken = "";
            UserRepository.Update(user);
            Logger.Information($"Пароль користувача було зміненно, id={user.Id}.");
        }
        public void ChangeOldPassword(string userToken, string oldPassword, string newPassword)
        {
            var user = UserRepository.GetByUserTokenNotDeleted(userToken);
            if (user == null)
            {
                throw new NotFoundException("Сервер не визначив користувача по токену для зміни старого паролю користувача.");
            }
            if (!ProfileCondition.VerifyHashedPassword(user.Password, oldPassword))
            {
                throw new ValidationException("Пароль користувача не співпадає з паролем на сервері для заміни старого паролю.");
            }
            user.Password = ProfileCondition.HashPassword(newPassword);
            UserRepository.Update(user);
            Logger.Information($"Старий пароль користувача було зміненно, id={user.Id}.");
        }
    }
}
