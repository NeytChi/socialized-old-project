using Core;
using Serilog;
using System.Web;
using UseCases.Admins;
using Domain.Users;
using Domain.Admins;
using Domain.SessionComponents;
using UseCases.Packages;
using UseCases.Users.Commands;
using UseCases.Exceptions;
using Amazon.Runtime.Internal.Util;
using Domain.Packages;

namespace UseCases.Users
{
    public class UsersManager
    {
        public ILogger Logger;
        public ProfileCondition ProfileCondition = new ProfileCondition();
        public PackageCondition PackageCondition;
        private EmailMessanger EmailMessanger;
        private IUserRepository UserRepository;
        private EmailFollowerManager EmailFollowerManager;

        public UsersManager(ILogger logger,
            IUserRepository repository,
            MailSettings mailSettings,
            IEmailFollowerRepository followerRepository)
        {
            Logger = logger;
            UserRepository = repository;
            EmailMessanger = new EmailMessanger(new SmtpSender(logger, mailSettings));
            PackageCondition = new PackageCondition(context, logger);
            EmailFollowerManager = new EmailFollowerManager(followerRepository, logger);
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
            PackageCondition.CreateFreeAccess(user.Id);
            EmailMessanger.SendConfirmEmail(user.Email, command.Culture, user.HashForActivate);
            Logger.Information($"Новий користувач був створений, id={user.Id}.");
        }
        public User GetByHash(string userHash, ref string message)
        {
            if (userHash != null)
            {
                var user = UserRepository.GetByHash(userHash, false, false);
                if (user != null)
                {
                    return user;
                }
            }
            Logger.Information("Server can't define user by hash.");
            message = "define_hash";
            return null;
        }
        public User GetNonDeleted(string email, ref string message)
        {
            if (email != null)
            {
                var user = UserRepository.GetByEmail(email, false);
                if (user != null)
                {
                    return user;
                }
            }
            Logger.Information("Server can't define user by email.");
            message = "define_email";
            return null;
        }
        public User GetByToken(string userToken, ref string message)
        {
            if (userToken != null)
            {
                var user = UserRepository.GetByUserTokenNotDeleted(userToken);
                return user;
            }
            Logger.Information("Server can't define user by promotion token.");
            message = "define_token";
            return null;
        }
        public User GetByRecoveryToken(string recoveryToken, ref string message)
        {
            if (recoveryToken != null)
            {
                var user = UserRepository.GetByRecoveryToken(recoveryToken, false);
                return user;
            }
            Logger.Information("Server can't define user by promotion token.");
            message = "define_token";
            return null;
        }

        public User GetActivateNonDeleted(string userEmail, ref string message)
        {
            if (userEmail != null)
            {
                var user = UserRepository.GetByEmail(userEmail, false, true);
                return user;
            }
            Logger.Information("Server can't define user by email.");
            message = "define_email";
            return null;
        }
        public bool CheckUserData(UserCache user, ref string message)
        {
            if (!string.IsNullOrEmpty(user.user_email))
            {
                if (!string.IsNullOrEmpty(user.user_password))
                {
                    return true;
                }
                else
                {
                    message = "empty_password";
                }
            }
            else
            {
                message = "empty_email";
            }
            return false;
        }
        public User Login(UserCache cache, ref string message)
        {
            if (CheckUserData(cache, ref message))
            {
                var user = GetActivateNonDeleted(cache.user_email, ref message);
                if (user != null)
                {
                    if (ProfileCondition.VerifyHashedPassword(user.userPassword, cache.user_password))
                    {
                        user.lastLoginAt = DateTimeOffset.Now.ToUnixTimeSeconds();
                        UserRepository.Update(user);
                        Logger.Information("User login, id -> " + user.userId);
                        return user;
                    }
                    else
                    {
                        message = "wrong_password";
                    }
                }
                else
                {
                    message = "define_email";
                }
            }
            return null;
        }
        public bool LogOut(string userToken, ref string message)
        {
            var user = GetByToken(userToken, ref message);
            if (user != null)
            {
                user.userToken = ProfileCondition.CreateHash(40);
                UserRepository.Update(user);
                Logger.Information("User log out, id -> " + user.userId);
                return true;
            }
            return false;
        }
        public bool RecoveryPassword(string userEmail, string culture, ref string message)
        {
            var user = GetActivateNonDeleted(userEmail, ref message);
            if (user != null)
            {
                user.recoveryCode = ProfileCondition.CreateCode(6);
                UserRepository.Update(user);
                EmailMessanger.SendRecoveryEmail(user.userEmail, culture, (int)user.recoveryCode);
                Logger.Information("Recovery password, id ->" + user.userId);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Confirm that current user get recovery code and ready to change his password.
        /// </summary>
        /// <return>Recovery token - for access to change password.</return>
        public string CheckRecoveryCode(string userEmail, int recoveryCode, ref string message)
        {
            var user = GetActivateNonDeleted(userEmail, ref message);
            if (user != null)
            {
                if (user.recoveryCode == recoveryCode)
                {
                    user.recoveryToken = ProfileCondition.CreateHash(40);
                    user.recoveryCode = -1;
                    UserRepository.Update(user);
                    Logger.Information("Check user's recovery code, id ->" + user.userId);
                    return user.recoveryToken;
                }
                else
                {
                    Logger.Information("Wrong recovery code.");
                    message = "wrong_code";
                }
            }
            return null;
        }
        public bool ChangePassword(string recoveryToken, string userPassword, string userConfirmPassword, ref string message)
        {
            var user = GetByRecoveryToken(recoveryToken, ref message);
            if (user != null)
            {
                if (userPassword.Equals(userConfirmPassword))
                {
                    if (ProfileCondition.PasswordIsTrue(userPassword, ref message))
                    {
                        user.userPassword = ProfileCondition.HashPassword(userPassword);
                        user.recoveryToken = "";
                        UserRepository.Update(user);
                        Logger.Information("Change user password, id ->" + user.userId);
                        return true;
                    }
                }
                else
                {
                    Logger.Information("Password are not match to each other.");
                    message = "match_password";
                }
            }
            return false;
        }
        public bool ChangeOldPassword(string userToken, string oldPassword, string newPassword, ref string message)
        {
            User user = GetByToken(userToken, ref message);

            if (user != null)
            {
                if (ProfileCondition.VerifyHashedPassword(user.userPassword, oldPassword))
                {
                    if (ProfileCondition.PasswordIsTrue(newPassword, ref message))
                    {
                        user.userPassword = ProfileCondition.HashPassword(newPassword);
                        UserRepository.Update(user);
                        Logger.Information("Change old password, id -> " + user.userId);
                        return true;
                    }
                }
                else
                {
                    message = "wrong_password";
                }
            }
            return false;
        }
        public bool RegistrationEmail(string userEmail, string culture, ref string message)
        {
            var user = GetNonDeleted(userEmail, ref message);
            if (user != null)
            {
                EmailMessanger.SendConfirmEmail(user.userEmail, culture, user.userHash);
                Logger.Information("Send registration email to user, id ->" + user.userId);
                return true;
            }
            return false;
        }
        public bool Activate(string hash, ref string message)
        {
            var user = GetByHash(hash, ref message);
            if (user != null)
            {
                user.activate = true;
                UserRepository.Update(user);
                Logger.Information("Active user account, id ->" + user.userId);
                EmailFollowerManager.BindWithFollower(user.userEmail, user.userId);
                return true;
            }
            return false;
        }

        public bool Delete(string userToken, ref string message)
        {
            User user = GetByToken(userToken, ref message);
            if (user != null)
            {
                user.deleted = true;
                user.userToken = null;
                UserRepository.Update(user);
                Logger.Information("Account was successfully deleted, id ->" + user.userId);
                DeleteUsersData(user.userId);
                return true;
            }
            return false;
        }
    }
}