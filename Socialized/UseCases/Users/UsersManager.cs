using Core;
using Serilog;
using System.Web;
using UseCases.Admins;
using Domain.Users;
using Domain.Admins;
using Domain.SessionComponents;
using UseCases.Packages;

namespace UseCases.Users
{
    public class UsersManager
    {
        public ILogger Logger;
        public ProfileCondition ProfileCondition;
        public PackageCondition PackageCondition;
        private EmailMessanger EmailMessanger;
        private IUserRepository Repository;
        private EmailFollowerManager EmailFollowerManager;

        public UsersManager(ILogger logger,
            IUserRepository repository,
            MailSettings mailSettings,
            IEmailFollowerRepository followerRepository)
        {
            Logger = logger;
            Repository = repository;
            ProfileCondition = new ProfileCondition(logger);
            EmailMessanger = new EmailMessanger(new SmtpSender(logger, mailSettings));
            PackageCondition = new PackageCondition(context, logger);
            EmailFollowerManager = new EmailFollowerManager(followerRepository, logger);
        }
        public bool RegistrationUser(UserCache cache, ref string message)
        {
            if (!ProfileCondition.EmailIsTrue(cache.user_email, ref message))
            {
                return false;
            }
            if (!ProfileCondition.PasswordIsTrue(cache.user_password, ref message))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(cache.country))
            {
                Logger.Information("Country name is empty.");
                message = "empty_country";
                return false;
            }
            if (!(cache.country.Length > 0 && cache.country.Length < 100))
            {
                Logger.Information("Country name is required length from 0 to 100 symbols.");
                message = "length_country";
                return false;
            }
            if (!ProfileCondition.UserNameIsTrue(cache.user_fullname, ref message))
            {
                return false;
            }
            var currentUser = Repository.GetByEmail(cache.user_email);
            if (currentUser == null)
            {
                currentUser = Registrate(cache);
                EmailMessanger.SendConfirmEmail(currentUser.userEmail, cache.culture, currentUser.userHash);
                return true;
            }
            return RestoreUser(currentUser, ref message);
        }
        public bool RestoreUser(User user, ref string message)
        {
            if (user.deleted)
            {
                user.deleted = false;
                user.userToken = ProfileCondition.CreateHash(40);
                Repository.Update(user);
                Logger.Information("User account was restored, id -> " + user.userId);
                return true;
            }
            else
            {
                message = "exist_email";
            }
            Logger.Warning("This email already exists.");
            return false;
        }
        public User Registrate(UserCache cache)
        {
            var user = new User
            {
                userEmail = cache.user_email,
                userFullName = HttpUtility.UrlDecode(cache.user_fullname),
                userPassword = ProfileCondition.HashPassword(cache.user_password),
                userHash = ProfileCondition.CreateHash(100),
                createdAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
                lastLoginAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
                userToken = ProfileCondition.CreateHash(40),
                profile = new Profile
                {
                    country = HttpUtility.UrlDecode(cache.country),
                    timezone = cache.timezone_seconds
                },
                access = new ServiceAccess()
            };
            Repository.Create(user);
            PackageCondition.CreateFreeAccess(user.userId);
            EmailFollowerManager.UpdateExistFollower(user.userEmail, user.userId);
            Logger.Information("Registrate new user, id -> " + user.userId);
            return user;
        }
        
        public User GetByHash(string userHash, ref string message)
        {
            if (userHash != null)
            {
                var user = Repository.GetByHash(userHash, false, false);
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
                var user = Repository.GetByEmail(email, false);
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
                var user = Repository.GetByUserTokenNotDeleted(userToken);
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
                var user = Repository.GetByRecoveryToken(recoveryToken, false);
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
                var user = Repository.GetByEmail(userEmail, false, true);
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
                        Repository.Update(user);
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
                Repository.Update(user);
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
                Repository.Update(user);
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
                    Repository.Update(user);
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
                        Repository.Update(user);
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
                        Repository.Update(user);
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
                Repository.Update(user);
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
                Repository.Update(user);
                Logger.Information("Account was successfully deleted, id ->" + user.userId);
                DeleteUsersData(user.userId);
                return true;
            }
            return false;
        }
    }
}