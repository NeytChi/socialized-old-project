using System.IO;
using Managment;
using Controllers;
using System.Linq;
using Models.Common;
using NUnit.Framework;
using database.context;
using Models.SessionComponents;
using Microsoft.AspNetCore.Mvc;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestIGAccountsController
    {
        public TestIGAccountsController()
        {
            context = TestMockingContext.GetContext();
            this.manager = new SessionManager(context);
            controller = new IGAccountsController(context);
            controller.sessionManager = manager;
        }
        public SessionManager manager;
        string accountPath = "/home/neytchi/project/service-ag/socialized/testing/test-mocking/nikachikapika2";
        public Context context;
        public IGAccountsController controller;
        public string igUsername = "nikachikapika2";
        public string igPassword = "Pass1235";

        [Test]
        public void Add()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User user = TestMockingContext.CreateUser();
                InstagramUser igUser = new InstagramUser();
                igUser.user_token = user.userToken;
                igUser.instagram_username = igUsername;
                igUser.instagram_password = igPassword;
                ActionResult<dynamic> jsonMessage = controller.Add(igUser);
                IGAccount account = context.IGAccounts.Where(s => s.userId == user.userId).First();
                ActionResult<dynamic> unsuccess = controller.Add(igUser);
                Assert.AreEqual(jsonMessage.Value.success, true);
                Assert.AreEqual(unsuccess.Value.success, false);
            }
        }
        [Test]
        public void Delete()
        {
            User user = TestMockingContext.CreateUser();
            IGAccount account = TestMockingContext.CreateSession(user.userId);
            InstagramUser igUser = new InstagramUser();
            igUser.user_token = user.userToken;
            igUser.session_id = account.accountId;
            ActionResult<dynamic> success = controller.Delete(igUser);
            ActionResult<dynamic> unsuccess = controller.Delete(igUser);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        public void SmsVerify()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                string verifyCode = "1";
                User user = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(user.userId);
                InstagramUser igUser = new InstagramUser();
                igUser.user_token = user.userToken;
                igUser.session_id = account.accountId;
                igUser.verify_code = verifyCode;
                ActionResult<dynamic> success = controller.SmsVerify(igUser);
                ActionResult<dynamic> unsuccess = controller.SmsVerify(igUser);
                Assert.AreEqual(success.Value.success, true);
                Assert.AreEqual(unsuccess.Value.success, false);
            }
        }
        [Test]
        public void Relogin()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User user = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(user.userId);
                account.accountUsername = igUsername;
                account.sessionSave = manager.Encrypt(File.ReadAllText(accountPath));
                account.State.stateUsable = false;
                account.State.stateRelogin = true;
                context.IGAccounts.Update(account);
                context.States.Update(account.State);
                context.SaveChanges();
                InstagramUser igUser = new InstagramUser();
                igUser.user_token = user.userToken;
                igUser.instagram_username = igUsername;
                igUser.instagram_password = igPassword;
                ActionResult<dynamic> success = controller.Relogin(igUser);
                ActionResult<dynamic> unsuccess = controller.Relogin(igUser);
                Assert.AreEqual(success.Value.success, true);
                Assert.AreEqual(unsuccess.Value.success, false);
            }
        }
        [Test]
        public void GetSessions()
        {
            User user = TestMockingContext.CreateUser();
            IGAccount account = TestMockingContext.CreateSession(user.userId);
            InstagramUser igUser = new InstagramUser();
            igUser.user_token = user.userToken;
            ActionResult<dynamic> success = controller.GetSessions(igUser);
            igUser.user_token = "";
            ActionResult<dynamic> unsuccess = controller.GetSessions(igUser);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
    }
}