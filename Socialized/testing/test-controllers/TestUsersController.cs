using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;

using Controllers;
using Models.Common;
using database.context;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestUsersController
    { 
        public TestUsersController()
        {
            context = TestMockingContext.GetContext();
            controller = new UsersController(context);
        }
        public Context context;
        public UsersController controller;

        [Test]
        public void Registration()
        {
            TestMockingContext.DeleteUser();
            UserCache cache = new UserCache();
            cache.user_email = TestMockingContext.values.user_email;
            cache.user_password = "Pass1234";
            cache.country = "UK, London";
            cache.timezone_seconds = 1000;
            ActionResult<dynamic> result = controller.Registration(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void RegistrationWrong()
        {
            TestMockingContext.DeleteUser();
            UserCache cache = new UserCache();
            cache.user_email = "";
            cache.user_password = "Pass1234";
            cache.country = "UK, London";
            cache.timezone_seconds = 1000;
            ActionResult<dynamic> result = controller.Registration(cache);
            Assert.AreEqual(result.Value.success, false);
        }
        [Test]
        public void Activate()
        {
            User user = TestMockingContext.CreateUser();
            user.userHash = "hash";
            user.activate = false;
            context.Users.Update(user);
            context.SaveChanges();
            ActionResult<dynamic> result = controller.Activate(user.userHash);
            user.userHash = "";
            ActionResult<dynamic> unsuccess = controller.Activate(user.userHash);
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void RegistrationEmail()
        {
            User userCache = TestMockingContext.CreateUser();
            UserCache user = new UserCache();
            user.user_email = userCache.userEmail;
            ActionResult<dynamic> result = controller.RegistrationEmail(user);
            user.user_email = null;
            ActionResult<dynamic> unsuccess = controller.RegistrationEmail(user);
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void Login()
        {
            User userCache = TestMockingContext.CreateUser();
            UserCache user = new UserCache();
            user.user_email = userCache.userEmail;
            user.user_password = TestMockingContext.values.user_password;
            ActionResult<dynamic> result = controller.Login(user);
            user.user_password = null;
            ActionResult<dynamic> unsuccess = controller.Login(user);
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void LogOut()
        {
            User userCache = TestMockingContext.CreateUser();
            UserCache user = new UserCache();
            user.user_token = userCache.userToken;
            ActionResult<dynamic> result = controller.LogOut(user);
            ActionResult<dynamic> unsuccess = controller.LogOut(user);
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void RecoveryPassword()
        {
            User userCache = TestMockingContext.CreateUser();
            UserCache user = new UserCache();
            user.user_email = userCache.userEmail;
            ActionResult<dynamic> result = controller.RecoveryPassword(user);    
            user.user_email = null;
            ActionResult<dynamic> unsuccess = controller.RecoveryPassword(user);
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void CheckRecoveryCode()
        {
            User userCache = TestMockingContext.CreateUser();
            userCache.recoveryCode = 1;
            context.Users.Update(userCache);
            context.SaveChanges();
            UserCache user = new UserCache();
            user.user_email = userCache.userEmail;
            user.recovery_code = 1;
            ActionResult<dynamic> result = controller.CheckRecoveryCode(user);
            UserCache userWithoutRecoveryCode = new UserCache();
            userWithoutRecoveryCode.user_email = userCache.userEmail;
            ActionResult<dynamic> unsuccess = controller.CheckRecoveryCode(userWithoutRecoveryCode);
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void ChangePassword()
        {
            User userCache = TestMockingContext.CreateUser();
            userCache.recoveryToken = "token";
            context.Users.Update(userCache);
            context.SaveChanges();
            UserCache user = new UserCache();
            user.recovery_token = userCache.recoveryToken;
            user.user_password = "Pass1234";
            user.user_confirm_password = "Pass1234";
            ActionResult<dynamic> result = controller.ChangePassword(user);  
            user.recovery_token = null; 
            ActionResult<dynamic> unsuccess = controller.ChangePassword(user);
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void Delete()
        {
            User userCache = TestMockingContext.CreateUser();
            UserCache user = new UserCache();
            user.user_token = userCache.userToken;
            ActionResult<dynamic> result = controller.Delete(user);   
            user.user_token = null;
            ActionResult<dynamic> unsuccess = controller.Delete(user);   
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
    }
}