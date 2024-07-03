using Serilog;
using System.Linq;
using NUnit.Framework;

using socialized;
using Managment;
using Controllers;
using database.context;
using Models.AdminPanel;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestAdminController
    {
        Context context;
        AdminsController controller;

        public TestAdminController()
        {
            AuthOptions authOptions = new AuthOptions();
            this.context = TestMockingContext.GetContext();
            this.controller = new AdminsController();
            this.controller.admins = new Admins(new LoggerConfiguration().CreateLogger(), this.context);
        }
        [Test]
        public void SignIn()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = TestMockingContext.CreateAdmin();
            var result = controller.SignIn(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void CreateAdmin()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = TestMockingContext.CreateAdmin();
            var result = controller.SignIn(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void SetupPassword()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = TestMockingContext.CreateAdmin();
            cache.confirm_password = cache.admin_password;
            cache.password_token = admin.passwordToken;
            var result = controller.SetupPassword(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void RecoveryPassword()
        {
            string error = string.Empty;
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = controller.admins.CreateAdmin(cache, ref error);
            cache.admin_email = admin.adminEmail;
            var result = controller.RecoveryPassword(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void ChangePassword()
        {
            string error = string.Empty;
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = controller.admins.CreateAdmin(cache, ref error);
            cache.admin_email = admin.adminEmail;
            controller.RecoveryPassword(cache);
            admin = context.Admins.Where(a => a.adminId == admin.adminId).First();
            cache.recovery_code = (int)admin.recoveryCode;
            cache.admin_password = TestMockingContext.values.admin_password;
            cache.confirm_password = cache.admin_password;
            var result = controller.ChangePassword(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void GetAdmins()
        {
            Admin admin = TestMockingContext.CreateAdmin();
            var result = controller.GetAdmins(0, 1);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void GetFollowers()
        {
            Admin admin = TestMockingContext.CreateAdmin();
            TestMockingContext.CreateFollower();
            var result = controller.GetFollowers(0, 1);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void GetUsers()
        {
            Admin admin = TestMockingContext.CreateAdmin();
            TestMockingContext.CreateUser();
            var result = controller.GetUsers(0, 1);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void DeleteAdmin()
        {
            AdminCache cache = TestMockingContext.GetAdminCache();
            Admin admin = TestMockingContext.CreateAdmin();
            cache.admin_id = admin.adminId;
            var result = controller.DeleteAdmin(cache);
            Assert.AreEqual(result.Value.success, true);
        }
    }
}