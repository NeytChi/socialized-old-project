
using System;
using Serilog;
using System.Linq;
using NUnit.Framework;

using Common;
using Managment;
using database.context;
using Models.Common;
using Models.Lending;

namespace Testing.Managment
{
    [TestFixture]
    public class TestUserManager
    {
        public string message = null;
        public string userEmail = "test6574839210@gmail.com";
        public string userPassword = "Pass1234";
        public string token = "1234";
        public int code = 123456;
        public Context context;
        public UsersManager usersManager;
        ProfileCondition condition;
        public TestUserManager()
        {
            context = TestMockingContext.GetContext();
            this.usersManager = new UsersManager(new LoggerConfiguration().CreateLogger(), context);
            this.condition = new ProfileCondition(usersManager.log);
        }
        [Test]
        public void RegistrationUser()
        {
            DeleteExistUser();
            UserCache user = new UserCache();
            user.country = "New York";
            user.timezone_seconds = 1000;
            user.user_email = userEmail;
            user.user_password = userPassword;
            bool success = usersManager.RegistrationUser(user, ref message);
            bool unsuccess = usersManager.RegistrationUser(user, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void ProfileIsTrue()
        {
            UserCache user = new UserCache(){
                country = "New York",
                timezone_seconds = 1000
            };
            Assert.AreEqual(usersManager.ProfileIsTrue(user, ref message), true);
        }
        [Test]
        public void ProfileIsTrueButEmpty()
        {
            message = "";
            UserCache cache = new UserCache(){
                country = "",
                timezone_seconds = 1000
            };
            Assert.AreEqual(usersManager.ProfileIsTrue(cache, ref message), false);
            Assert.AreEqual(string.IsNullOrEmpty(message), false);
        }
        [Test]
        public void RestoreUser()
        {
            User user = CreateUser();
            user.deleted = true;
            context.Users.Update(user);
            context.SaveChanges();
            bool success = usersManager.RestoreUser(user,ref message);
            bool unsuccess = usersManager.RestoreUser(user,ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void Registrate()
        {
            DeleteExistUser();
            UserCache cache = new UserCache(){
                user_email = userEmail,
                user_password = userPassword,
                country = "City",
                timezone_seconds = 1000
            };
            Assert.AreEqual(usersManager.Registrate(cache).profile.country.Equals(cache.country), true);
        }

        [Test]
        public void SendConfirmEmail()
        {
            usersManager.SendConfirmEmail(userEmail, "", "test_hash");
        }
        [Test]
        public void SendRecoveryEmail()
        {
            usersManager.SendRecoveryEmail(userEmail, "", 1234);
        }
        [Test]
        public void ValidateUser()
        {
            bool success = usersManager.ValidateUser(userEmail, userPassword, ref message);
            bool unsuccess = usersManager.ValidateUser("", "", ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetByHash()
        {
            User user = CreateUser();
            user.userHash = token;
            context.Users.Update(user);
            context.SaveChanges();
            User selected = usersManager
            .GetByHash(token, ref message);
            DeleteExistUser();
            User nonSelected = usersManager
            .GetByHash(token, ref message);
            Assert.AreNotEqual(selected, null);
            Assert.AreEqual(nonSelected, null);
        }
        [Test]
        public void GetNonDeleted()
        {
            User user = CreateUser();
            User selected = usersManager.GetNonDeleted(userEmail, ref message);
            user.deleted = true;
            context.Users.Update(user);
            context.SaveChanges();
            User nonSelected = usersManager.GetNonDeleted(userEmail, ref message);
            DeleteExistUser();
            Assert.AreNotEqual(selected, null);
            Assert.AreEqual(nonSelected, null);
        }
        [Test]
        public void GetByToken()
        {
            User user = CreateUser();
            User selected = usersManager.GetByToken(token, ref message);
            user.deleted = true;
            context.Users.Update(user);
            context.SaveChanges();
            User nonSelected = usersManager.GetByToken(token, ref message);
            DeleteExistUser();
            Assert.AreNotEqual(selected, null);
            Assert.AreEqual(nonSelected, null);
        }
        [Test]
        public void GetByRecoveryToken()
        {
            User user = CreateUser();
            user.recoveryToken = token;
            context.Users.Update(user);
            context.SaveChanges();
            User selected = usersManager.GetByRecoveryToken(token, ref message);
            DeleteExistUser();
            User nonSelected = usersManager.GetByRecoveryToken(token, ref message);
            Assert.AreNotEqual(selected, null);
            Assert.AreEqual(nonSelected, null);
        }
        [Test]
        public void GetActivateNonDeleted()
        {
            User user = CreateUser();
            user.activate = true;
            context.Users.Update(user);
            context.SaveChanges();
            User selected = usersManager.GetActivateNonDeleted(userEmail, ref message);
            user.activate = false;
            context.Users.Update(user);
            context.SaveChanges();
            User nonSelected = usersManager.GetActivateNonDeleted(userEmail, ref message);
            DeleteExistUser();
            Assert.AreNotEqual(selected, null);
            Assert.AreEqual(nonSelected, null);
        }
        [Test]
        public void CheckUserData()
        {
            UserCache user = new UserCache();
            user.user_email = userEmail;
            user.user_password = userPassword;
            bool success = usersManager.CheckUserData(user, ref message);
            user.user_password = null;
            bool unsuccess = usersManager.CheckUserData(user, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void Login()
        {
            User userCache = CreateUser();
            userCache.activate = true;
            context.Users.Update(userCache);
            context.SaveChanges();
            UserCache user = new UserCache();
            user.user_email = userEmail;
            user.user_password = userPassword;
            User success = usersManager.Login(user, ref message);
            user.user_password = null;
            User unsuccess = usersManager.Login(user, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void LogOut()
        {
            User user = CreateUser();
            user.activate = true;
            context.Users.Update(user);
            context.SaveChanges();
            bool success = usersManager.LogOut(token, ref message);
            bool unsuccess = usersManager.LogOut(token, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void RecoveryPassword()
        {
            User user = CreateUser();
            user.activate = true;
            context.Users.Update(user);
            context.SaveChanges();
            bool success = usersManager.RecoveryPassword(userEmail, "", ref message);
            bool unsuccess = usersManager.RecoveryPassword(null, "", ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckRecoveryCode()
        {
            User user = CreateUser();
            user.recoveryCode = code;
            user.activate = true;
            user.deleted = false;
            context.Users.Update(user);
            context.SaveChanges();
            string success = usersManager.CheckRecoveryCode(userEmail, code, ref message);            
            string unsuccess = usersManager.CheckRecoveryCode(userEmail, code, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void ChangePassword()
        {
            User user = CreateUser();
            user.recoveryToken = token;
            context.Users.Update(user);
            context.SaveChanges();
            bool success = usersManager.ChangePassword
            (token, userPassword, userPassword, ref message);
            bool unsuccess = usersManager.ChangePassword(
            token, userPassword, userPassword, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void RegistrationEmail()
        {
            bool success = usersManager
            .RegistrationEmail(userEmail, "", ref message);
            bool unsuccess = usersManager
            .RegistrationEmail(null, "", ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void Activate()
        {
            User user = CreateUser();
            user.userHash = token;
            context.Users.Update(user);
            context.SaveChanges();
            bool success = usersManager.Activate(
            token, ref message);
            bool unsuccess = usersManager.Activate(
            token, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void Delete()
        {
            User user = CreateUser();            
            bool success = usersManager.Delete(token, ref message);
            bool unsuccess = usersManager.Delete(token, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void DeleteUsersData()
        {
            User user = CreateUser();            
            usersManager.DeleteUsersData(user.userId);
        }
        [Test]
        public void BindWithFollower()
        {
            User user = CreateUser();
            Follower follower = new Follower() {
                followerEmail = user.userEmail,
                createdAt = DateTime.Now,
                enableMailing = true 
            };
            context.Followers.Add(follower);
            user.activate = true;
            context.Users.Update(user);
            context.SaveChanges();
            usersManager.BindWithFollower(user);
        }
        [Test]
        public void Try_BindWithFollower_If_Not_Exist()
        {
            User user = CreateUser();
            user.activate = true;
            context.Users.Update(user);
            context.SaveChanges();
            usersManager.BindWithFollower(user);
        }
        [Test]
        public void Try_BindWithFollower_Not_Activate_Account()
        {
            User user = CreateUser();
            Follower follower = new Follower() {
                followerEmail = user.userEmail,
                createdAt = DateTime.Now,
                enableMailing = true 
            };
            context.Followers.Add(follower);
            context.SaveChanges();
            usersManager.BindWithFollower(user);
        }
        [Test]
        public void Try_BindWithFollower_Deleted_Account()
        {
            User user = CreateUser();
            Follower follower = new Follower() {
                followerEmail = user.userEmail,
                createdAt = DateTime.Now,
                enableMailing = true 
            };
            context.Followers.Add(follower);
            user.deleted = true;
            context.Users.Update(user);
            context.SaveChanges();
            usersManager.BindWithFollower(user);
        }
        public User CreateUser()
        {
            DeleteExistUser();
            User user = new User();
            user.userEmail = userEmail;
            user.userPassword = condition.HashPassword(userPassword);
            user.userToken = token;
            context.Users.Add(user);
            context.SaveChanges();
            return user;
        }
        public void DeleteExistUser()
        {
            User user = context.Users.Where(u => u.userEmail == userEmail).FirstOrDefault();
            if (user != null)
            {
                context.Users.Remove(user);
                context.SaveChanges();
            }
        }
    }
}