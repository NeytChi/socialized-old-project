using System;
using System.Web;
using System.IO;
using System.Linq;
using Serilog.Core;
using Microsoft.Extensions.Configuration;

using Common;
using socialized;
using database.context;
using Models.Common;
using Models.Lending;
using Models.SessionComponents;
using Models.GettingSubscribes;

namespace Managment
{
    public class UsersManager
    {
        public Logger log;
        public ProfileCondition validator;
        public PackageCondition access;
        private Context context;
        private MailF mail;
        private string emailActivateUrl;
        private string emailPath;
        public UsersManager(Logger log, Context context)
        {
            this.log = log;
            this.context = context;
            this.validator = new ProfileCondition(log);
            this.mail = new MailF(log);
            this.access = new PackageCondition(context, log);
            this.emailActivateUrl = Program.serverConfiguration().GetValue<string>("email_activate_url");
            this.emailPath = Program.serverConfiguration().GetValue<string>("email_path");
        }
        public UsersManager(Logger log)
        {
            this.log = log;
            this.context = new Context(false);
            this.validator = new ProfileCondition(log);
            this.mail = new MailF(log);
            this.access = new PackageCondition(context, log);
            this.emailActivateUrl = Program.serverConfiguration().GetValue<string>("email_activate_url");
            this.emailPath = Program.serverConfiguration().GetValue<string>("email_path");
        }
        public bool RegistrationUser(UserCache cache, ref string message)
        {
            if (ValidateUser(cache.user_email, cache.user_password, ref message) 
                && ProfileIsTrue(cache, ref message)
                && validator.UserNameIsTrue(cache.user_fullname, ref message)) {
                User currentUser = context.Users.Where(u => u.userEmail == cache.user_email).FirstOrDefault();
                if (currentUser == null) {
                    currentUser = Registrate(cache);
                    SendConfirmEmail(currentUser.userEmail, cache.culture, currentUser.userHash);
                    return true;
                }
                else
                    return RestoreUser(currentUser, ref message);
            }
            return false;
        }
        public bool RestoreUser(User user, ref string message)
        {
            if (user.deleted) {
                user.deleted = false;
                user.userToken = validator.CreateHash(40);
                context.Users.Update(user);
                context.SaveChanges();
                log.Information("User account was restored, id -> " + user.userId);
                return true;
            }
            else 
                message = "exist_email";
            log.Warning("This email already exists.");
            return false;
        }
        public User Registrate(UserCache cache)
        {
            User user = new User();
            user.userEmail = cache.user_email;
            user.userFullName = HttpUtility.UrlDecode(cache.user_fullname);
            user.userPassword = validator.HashPassword(cache.user_password);
            user.userHash = validator.CreateHash(100);
            user.createdAt = DateTimeOffset.Now.ToUnixTimeSeconds();
            user.lastLoginAt = DateTimeOffset.Now.ToUnixTimeSeconds();
            user.userToken = validator.CreateHash(40);
            user.profile = new Profile();
            user.profile.country = HttpUtility.UrlDecode(cache.country);
            user.profile.timezone = cache.timezone_seconds;
            user.access = new ServiceAccess();
            context.Users.Add(user);
            context.SaveChanges();
            access.CreateFreeAccess(user.userId);
            UpdateExistFollower(user);
            log.Information("Registrate new user, id -> " + user.userId);
            return user;
        }
        public void UpdateExistFollower(User user)
        {
            Follower follower = context.Followers.Where(f 
                => f.followerEmail == user.userEmail).FirstOrDefault();
            if (follower != null) {
                follower.userId = user.userId;
                context.Followers.Update(follower);
                context.SaveChanges();
                log.Information("Updare exist followers, set user id, id -> " + follower.followerId);
            }
            log.Information("User doesn't have following on lending");
        }
        public void SendConfirmEmail(string userEmail, string culture, string userHash)
        {
            int indexValue = 0;
            string confirmAccount, confirmValue, emailText, positionText, emailName = "eng.html";

            positionText = "\' href=\"";
            confirmValue = emailActivateUrl + userHash;
            confirmAccount = context.Cultures.Where(c 
                => c.cultureKey == "confirm_account"
                && c.cultureName == culture).FirstOrDefault()?.cultureValue ?? "";
            if (culture == "ru_RU")
                emailName = "ru.html";
            emailText = File.ReadAllText(emailPath + emailName);

            indexValue = emailText.IndexOf(positionText);
            emailText = emailText.Insert(indexValue + positionText.Length, confirmValue);
            mail.SendEmail(userEmail, confirmAccount, emailText);
        }
        public void SendRecoveryEmail(string userEmail, string culture, int recoveryCode)
        {
            string recoveryPassword, tRecoveryCode;
            recoveryPassword = context.Cultures.Where(c 
                => c.cultureKey == "recovery_password"
                && c.cultureName == culture).FirstOrDefault()?.cultureValue ?? "";
            tRecoveryCode = context.Cultures.Where(c 
                => c.cultureKey == "recovery_code"
                && c.cultureName == culture).FirstOrDefault()?.cultureValue ?? "";
            mail.SendEmail(userEmail, recoveryPassword, tRecoveryCode + recoveryCode);
        }
        public bool ValidateUser(string userEmail, string userPassword, ref string message)
        {
            if (validator.EmailIsTrue(userEmail, ref message)) {
                if (validator.PasswordIsTrue(userPassword, ref message))
                    return true;
            }
            return false;
        }
        public User GetByHash(string userHash, ref string message)
        {
            if (userHash != null) {
                User user = context.Users.Where(
                    u => u.userHash == userHash
                    && u.activate == false
                    && u.deleted == false).FirstOrDefault();
                if (user != null)
                    return user;
            }
            log.Information("Server can't define user by hash."); 
            message = "define_hash";
            return null;
        }
        public User GetNonDeleted(string userEmail, ref string message)
        {
            if (userEmail != null) {
                User user = context.Users.Where(u 
                    => u.userEmail == userEmail
                    && u.deleted == false).FirstOrDefault();
                if (user != null)
                    return user;
            }
            log.Information("Server can't define user by email.");
            message = "define_email";
            return null;
        }
        public User GetByToken(string userToken, ref string message)
        {
            if (userToken != null) {
                User user = context.Users.Where(u 
                    => u.userToken == userToken
                    && u.deleted == false).FirstOrDefault();;
                return user;
            }
            log.Information("Server can't define user by promotion token.");
            message = "define_token"; 
            return null;
        }
        public User GetByRecoveryToken(string recoveryToken, ref string message)
        {
            if (recoveryToken != null) {
                User user = context.Users.Where(u 
                    => u.recoveryToken == recoveryToken
                    && u.deleted  == false).FirstOrDefault();;
                return user;
            }
            log.Information("Server can't define user by promotion token."); 
            message = "define_token"; 
            return null;
        }
        
        public User GetActivateNonDeleted(string userEmail, ref string message)
        {
            if (userEmail != null) {
                User user = context.Users.Where(u 
                => u.userEmail == userEmail
                && u.deleted == false
                && u.activate == true).FirstOrDefault();;
                return user;
            }
            log.Information("Server can't define user by email."); 
            message = "define_email";
            return null;
        }
        public bool CheckUserData(UserCache user, ref string message)
        {
            if (!string.IsNullOrEmpty(user.user_email)) {
                if (!string.IsNullOrEmpty(user.user_password))
                    return true;    
                else
                    message = "empty_password";
            }
            else 
                message = "empty_email";
            return false;
        }
        public User Login(UserCache cache, ref string message)
        {
            if (CheckUserData(cache, ref message)) {
                User user = GetActivateNonDeleted(cache.user_email, ref message);
                if (user != null) {
                    if (validator.VerifyHashedPassword(user.userPassword, cache.user_password)) {
                        user.lastLoginAt = DateTimeOffset.Now.ToUnixTimeSeconds();
                        context.Users.Update(user);
                        context.SaveChanges();
                        log.Information("User login, id -> " + user.userId);
                        return user;        
                    }
                    else 
                        message = "wrong_password"; 
                }
                else
                    message = "define_email"; 
            }
            return null;
        }
        public bool LogOut(string userToken, ref string message)
        {
            User user = GetByToken(userToken, ref message);
            if (user != null) {
                user.userToken = validator.CreateHash(40);
                context.Users.Update(user);
                context.SaveChanges();
                log.Information("User log out, id -> " + user.userId);
                return true;
            }
            return false;
        }
        public bool RecoveryPassword(string userEmail, string culture, ref string message)
        {
            User user = GetActivateNonDeleted(userEmail, ref message);
            if (user != null) {
                user.recoveryCode = validator.random.Next(100000, 999999);
                context.Users.Update(user);
                context.SaveChanges();
                SendRecoveryEmail(user.userEmail, culture, (int)user.recoveryCode);
                log.Information("Recovery password, id ->" + user.userId);
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
            User user = GetActivateNonDeleted(userEmail, ref message);
            if (user != null) {
                if (user.recoveryCode == recoveryCode) {
                    user.recoveryToken = validator.CreateHash(40);
                    user.recoveryCode = -1;
                    context.Users.Update(user);
                    context.SaveChanges();
                    log.Information("Check user's recovery code, id ->" + user.userId);
                    return user.recoveryToken;
                }
                else {
                    log.Information("Wrong recovery code.");
                    message = "wrong_code"; 
                }
            }
            return null;
        }
        public bool ChangePassword(string recoveryToken, string userPassword, string userConfirmPassword, ref string message)
        {
            User user = GetByRecoveryToken(recoveryToken, ref message);
            if (user != null) {
                if (userPassword.Equals(userConfirmPassword)) {
                    if (validator.PasswordIsTrue(userPassword, ref message)){
                        user.userPassword = validator.HashPassword(userPassword);
                        user.recoveryToken  = "";
                        context.Users.Update(user);
                        context.SaveChanges();
                        log.Information("Change user password, id ->" + user.userId);
                        return true;
                    }
                }
                else {
                    log.Information("Password are not match to each other.");
                    message = "match_password";
                }
            }
            return false;
        }
        public bool ChangeOldPassword(string userToken, string oldPassword, string newPassword, ref string message)
        {
            User user;

            if ((user = GetByToken(userToken, ref message)) != null) {
                if (validator.VerifyHashedPassword(user.userPassword, oldPassword)) {
                    if (validator.PasswordIsTrue(newPassword, ref message)) {
                        user.userPassword = validator.HashPassword(newPassword);
                        context.Users.Update(user);
                        context.SaveChanges();
                        log.Information("Change old password, id -> " + user.userId);
                        return true;
                    }
                }
                else 
                    message = "wrong_password";
            }
            return false;
        }
        public bool RegistrationEmail(string userEmail, string culture, ref string message)
        {
            User user = GetNonDeleted(userEmail, ref message);
            if (user != null) {
                SendConfirmEmail(user.userEmail, culture, user.userHash);
                log.Information("Send registration email to user, id ->" + user.userId);
                return true;
            }
            return false;
        }
        public bool Activate(string hash, ref string message)
        {
            User user = GetByHash(hash, ref message);
            if (user != null) {
                user.activate = true;
                context.Users.Update(user);
                context.SaveChanges();
                log.Information("Active user account, id ->" + user.userId);
                BindWithFollower(user);
                return true;
            }
            return false;
        }
        public void BindWithFollower(User user)
        {
            if (user.activate && !user.deleted) {
                Follower follower = context.Followers.Where(f 
                    => f.followerEmail == user.userEmail).FirstOrDefault();
                if (follower != null) {
                    follower.userId = user.userId;
                    context.Followers.Update(follower);
                    context.SaveChanges();
                }
            }
        }
        public bool Delete(string userToken, ref string message)
        {
            User user = GetByToken(userToken, ref message);
            if (user != null) {
                user.deleted = true;
                user.userToken = null;
                context.Users.Update(user);
                context.SaveChanges();
                log.Information("Account was successfully deleted, id ->" + user.userId); 
                DeleteUsersData(user.userId);
                return true;
            }
            return false;
        }
        public void DeleteUsersData(int userId)
        {
            IGAccount[] accounts = context.IGAccounts.Where(s 
                => s.userId == userId).ToArray();
            foreach(IGAccount account in accounts) {
                account.accountDeleted = true;
                TaskGS[] tasks = context.TaskGS.Where(t => t.sessionId == account.accountId).ToArray();
                foreach(TaskGS task in tasks) {
                    task.taskStopped = true;
                    task.taskDeleted = true;
                }
                context.UpdateRange(tasks);
                log.Information("Delete instagram account, id ->" + userId);
            }
            context.IGAccounts.UpdateRange(accounts);
            context.SaveChanges();
        }
        public bool ProfileIsTrue(UserCache cache, ref string message)
        {
            if (!string.IsNullOrEmpty(cache.country)) {
                if (cache.country.Length > 0 && cache.country.Length < 100)
                    return true;
                log.Information("Country name is required length from 0 to 100 symbols.");
                message = "length_country";
            }
            else {
                log.Information("Country name is empty.");
                message = "empty_country";
            }
            return false;
        }
    }
}