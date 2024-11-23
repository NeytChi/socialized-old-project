using Serilog;
using System.Linq;
using NUnit.Framework;

using Managment;
using socialized;
using database.context;
using Models.Common;
using Models.AdminPanel;
using Models.Lending;
using Controllers;

namespace Testing.Managment
{
    [TestFixture]
    public class TestAdmins
    {
        Admins admins;
        string error;
        Context context;
        LendingController lending;
            
        public TestAdmins()
        {
            AuthOptions authOptions = new AuthOptions();
            this.context = TestMockingContext.GetContext();
            this.admins = new Admins(new LoggerConfiguration().CreateLogger(), context);
            this.lending = new LendingController(context);
        }
        [Test]
        public void CreateAdmin()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            var success = admins.CreateAdmin(cache, ref error);
            Assert.AreEqual(success.adminEmail, cache.admin_email);
        }
        [Test]
        public void CreateAdminForConsole()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            var success = admins.CreateAdmin(cache, ref error);
            Assert.AreEqual(success.adminEmail, cache.admin_email);
        }
        [Test]
        public void CreateExistAdmin()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            admins.CreateAdmin(cache, ref error);
            var success = admins.CreateAdmin(cache, ref error);
            Assert.AreEqual(success, null);
        }
        [Test]
        public void AuthToken()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            cache.confirm_password = cache.admin_password;
            cache.password_token = admin.passwordToken;
            admins.SetupPassword(cache, ref error);
            var token = admins.AuthToken(cache, ref error);
            Assert.AreEqual(string.IsNullOrEmpty(token), false);
        }
        [Test]
        public void AuthTokenWithNonExistAdmin()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            var token = admins.AuthToken(cache, ref error);
            Assert.AreEqual(string.IsNullOrEmpty(token), true);
        }
        [Test]
        public void AuthTokenWithWrongPassword()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            cache.admin_password = "1234";
            var token = admins.AuthToken(cache, ref error);
            Assert.AreEqual(string.IsNullOrEmpty(token), true);
        }
        [Test]
        public void SetupPassword()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            cache.password_token = admin.passwordToken;
            cache.confirm_password = cache.admin_password;
            bool success = admins.SetupPassword(cache, ref error);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void SetupPasswordWithDiffentConfirmPassword()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            cache.password_token = admin.passwordToken;
            admins.SetupPassword(cache, ref error);
            bool success = admins.SetupPassword(cache, ref error);
            Assert.AreEqual(success, false);
        }
        [Test]
        public void SetupPasswordOnAdminWithAlreadyCreatedPassword()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            cache.password_token = admin.passwordToken;
            cache.confirm_password = cache.admin_password;
            bool success = admins.SetupPassword(cache, ref error);
            bool unsuccess = admins.SetupPassword(cache, ref error);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetNonDelete()
        {
            var emptyAdmin = admins.GetNonDelete("e@gmail.com", ref error);
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            var adminByEmail = admins.GetNonDelete(cache.admin_email, ref error);
            Assert.AreEqual(emptyAdmin, null);
            Assert.AreEqual(adminByEmail.adminId, admin.adminId);
        }
        [Test]
        public void DeleteAdmin()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            cache.admin_id = admin.adminId;
            bool success = admins.DeleteAdmin(cache, ref error);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void DeleteAdminNonExist()
        {
            AdminCache cache = new AdminCache();
            cache.admin_id = 0;
            bool success = admins.DeleteAdmin(cache, ref error);
            Assert.AreEqual(success, false);
        }
        [Test]
        public void GetNonDeleteAdmins()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            var result = admins.GetNonDeleteAdmins(0, 0, 5);
            Assert.AreEqual(result.Length, 1);
        }
        [Test]
        public void GetFollowers()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            TestMockingContext.CreateFollower();
            var result = admins.GetFollowers(0, 1);
            Assert.AreEqual(result.Length, 1);
            Assert.AreEqual(result[0].has_user, false);
        }
        [Test]
        public void GetFollower_With_User()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            User user = TestMockingContext.CreateUser();
            lending.FollowTo(new FollowerCache(){ follower_email = user.userEmail });
            var result = admins.GetFollowers(0, 1);
            Assert.AreEqual(result[0].has_user, true);
        }
        [Test]
        public void Try_Get_One_Follower_With_Delete_User()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            User user = TestMockingContext.CreateUser();
            user.deleted = true;
            context.Users.Update(user);
            context.SaveChanges();
            lending.FollowTo(new FollowerCache(){ follower_email = user.userEmail });
            var result = admins.GetFollowers(0, 1);
            Assert.AreEqual(result[0].has_user, false);
        }
        [Test]
        public void Try_Get_One_Follower_With_Non_Activate_User()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            User user = TestMockingContext.CreateUser();
            user.activate = false;
            context.Users.Update(user);
            context.SaveChanges();
            lending.FollowTo(new FollowerCache(){ follower_email = user.userEmail });
            var result = admins.GetFollowers(0, 1);
            Assert.AreEqual(result[0].has_user, false);
        }
        [Test]
        public void GetNonDeleteUsers()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            User user = TestMockingContext.CreateUser();
            var result = admins.GetNonDeleteUsers(0, 1);
            Assert.AreEqual(result.Length, 1);
            Assert.AreEqual(result[0].user_email, user.userEmail);
        }
        [Test]
        public void GetNonDeleteUsers_Non_Activate()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            User user = TestMockingContext.CreateUser();
            user.activate = false;
            context.Users.Update(user);
            context.SaveChanges();
            var result = admins.GetNonDeleteUsers(0, 1);
            Assert.AreEqual(result.Length, 0);
        }
        [Test]
        public void GetNonDeleteUsers_Non_Delete()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            User user = TestMockingContext.CreateUser();
            user.deleted = true;
            context.Users.Update(user);
            context.SaveChanges();
            var result = admins.GetNonDeleteUsers(0, 1);
            Assert.AreEqual(result.Length, 0);
        }
        [Test]
        public void RecoveryPassword()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            Assert.AreEqual(admins.RecoveryPassword(admin.adminEmail, ref error), true);
        }
        [Test]
        public void RecoveryPasswordEmail()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            admins.RecoveryPasswordMail(1234, cache.admin_email);
        }
        [Test]
        public void ChangePassword()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            admins.RecoveryPassword(admin.adminEmail, ref error);
            admin = context.Admins.Where(a => a.adminId == admin.adminId).First();
            cache.recovery_code = (int) admin.recoveryCode;
            cache.admin_password = TestMockingContext.values.admin_password;
            cache.confirm_password = cache.admin_password;
            Assert.AreEqual(admins.ChangePassword(cache, ref error), true);
        }
        [Test]
        public void ChangePassword_Not_Same_Passwords()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            admins.RecoveryPassword(admin.adminEmail, ref error);
            admin = context.Admins.Where(a => a.adminId == admin.adminId).First();
            cache.recovery_code = (int) admin.recoveryCode;
            cache.admin_password = TestMockingContext.values.admin_password;
            Assert.AreEqual(admins.ChangePassword(cache, ref error), false);
        }
        [Test]
        public void ChangePassword_Unknow_Recovery_Code()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            admins.RecoveryPassword(admin.adminEmail, ref error);
            admin = context.Admins.Where(a => a.adminId == admin.adminId).First();
            cache.recovery_code = (int) admin.recoveryCode;
            cache.admin_password = TestMockingContext.values.admin_password;
            Assert.AreEqual(admins.ChangePassword(cache, ref error), false);
        }
        [Test]
        public void ChangePassword_Bad_Passwords()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = admins.CreateAdmin(cache, ref error);
            admins.RecoveryPassword(admin.adminEmail, ref error);
            admin = context.Admins.Where(a => a.adminId == admin.adminId).First();
            cache.recovery_code = (int) admin.recoveryCode;
            cache.admin_password = "1234";
            Assert.AreEqual(admins.ChangePassword(cache, ref error), false);
        }
        [Test]
        public void CheckAdminInfo()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Assert.AreEqual(admins.CheckAdminInfo(cache, ref error), true);
        }
    }
}