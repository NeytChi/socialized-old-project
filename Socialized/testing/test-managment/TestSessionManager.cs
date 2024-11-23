using System.IO;
using NUnit.Framework;
using Managment;
using Models.Common;
using database.context;
using Models.SessionComponents;

using InstagramApiSharp.Classes;
using InstagramApiSharp.API.Builder;


namespace Testing.Managment
{
    [TestFixture]
    public class TestSessionManager
    { 
        public Context context;
        string accountPath = "/home/neytchi/project/service-ag/socialized/testing/test-mocking/nikachikapika2";
        public string instagramUsername = "nikachikapika2";
        public string instagramPassword = "Pass1235";
        public SessionManager manager;
        public string message;
        public TestSessionManager()
        {
            context = TestMockingContext.GetContext();
            manager = new SessionManager(context);
        }
        [Test]
        public void AddIGSession()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                TestMockingContext.DeleteAllSessions();
                User userCache = TestMockingContext.CreateUser();
                InstagramUser user = new InstagramUser() {
                    user_id = userCache.userId,
                    instagram_username = instagramUsername,
                    instagram_password = instagramPassword
                };
                var success = manager.AddInstagramSession(user, ref message);
                Assert.AreEqual(success.userId, userCache.userId);
            }
        }
        [Test]
        public void AddExistIGSession()
        {
            User user = TestMockingContext.CreateUser();
            IGAccount account = TestMockingContext.CreateSession(user.userId);
            InstagramUser cache = new InstagramUser() {
                user_id = user.userId,
                instagram_username = account.accountUsername,
                instagram_password = "Pass1234"
            };
            var result = manager.AddInstagramSession(cache, ref message);
            Assert.AreEqual(result, null);
        }
        [Test]
        public void RecoverySession()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                TestMockingContext.DeleteAllSessions();
                User userCache = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(userCache.userId);
                account.accountUsername = instagramUsername;
                account.sessionSave = manager.Encrypt(File.ReadAllText(accountPath));
                account.accountDeleted = true;
                context.IGAccounts.Update(account);
                context.SaveChanges();
                InstagramUser user = new InstagramUser() {
                    user_id = userCache.userId,
                    instagram_username = account.accountUsername,
                    instagram_password = instagramPassword
                };
                Assert.AreEqual(manager.RecoverySession(account, user, ref message).accountId, account.accountId);
                Assert.AreEqual(manager.RecoverySession(account, user, ref message), null);
            }
        }
        [Test]
        public void RestoreSession()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                TestMockingContext.DeleteAllSessions();
                User userCache = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(userCache.userId);
                InstagramUser user = new InstagramUser() {
                    user_id = userCache.userId,
                    instagram_username = instagramUsername,
                    instagram_password = instagramPassword
                };
                account.accountUsername = instagramUsername;
                account.sessionSave = manager.Encrypt(File.ReadAllText(accountPath));
                account.accountDeleted = true;
                account.State.stateRelogin = true;
                context.IGAccounts.Update(account);
                context.SaveChanges();
                IGAccount success = manager.RestoreSession(account, user, ref message);
                Assert.AreEqual(success.accountId, account.accountId);
                Assert.AreEqual(manager.RestoreSession(account, user, ref message), null);
            }
        }
        [Test]
        public void SetupSession()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                InstagramUser user = new InstagramUser();
                user.instagram_username = instagramUsername;
                user.instagram_password = instagramPassword;
                IGAccount success = manager.SetupSession(user, ref message);
                user.instagram_password = "";
                Assert.AreEqual(success.userId, user.user_id);
            }
        }
        [Test]
        public void ChallengeRequired()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User userCache = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(userCache.userId);
                account.accountUsername = instagramUsername;
                account.sessionSave = manager.Encrypt(File.ReadAllText(accountPath));
                context.IGAccounts.Update(account);
                context.SaveChanges();
                Session session = manager.LoadSession(account.accountId);
                bool success = manager.ChallengeRequired(ref session, true, ref message);
                Assert.AreEqual(success , true);
            }
        }
        [Test]
        public void SaveSession()
        {
            User user = TestMockingContext.CreateUser();
            Session session = new Session();
            session.User = new SessionData();
            session.User.UserName = instagramUsername;
            session.userId = user.userId;
            IGAccount result = manager.SaveSession(session, true, ref message);
            IGAccount emptyObj = manager.SaveSession(session, false, ref message);
            Assert.AreEqual(result.userId, user.userId);
            Assert.AreEqual(emptyObj, null);
        }
        [Test]
        public void UpdateUsableSession()
        {
            User user = TestMockingContext.CreateUser();
            IGAccount cache = TestMockingContext.CreateSession(user.userId);
            Session session = new Session();
            cache.sessionSave = File.ReadAllText(accountPath);
            manager.UpdateUsableSession(session, cache);
        }
        [Test]
        public void LoginSession()
        {
            Session session = new Session(instagramUsername, instagramPassword);
            var success = manager.LoginSession(session, ref message);
            Assert.AreEqual(success, InstaLoginResult.ChallengeRequired);
        }
        [Test]
        public void ReloginSession()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User userCache = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(userCache.userId);
                account.accountUsername = instagramUsername;
                account.sessionSave = manager.Encrypt(File.ReadAllText(accountPath));
                account.State.stateRelogin = true;
                context.IGAccounts.Update(account);
                context.SaveChanges();
                InstagramUser user = new InstagramUser() {
                    user_id = userCache.userId,
                    instagram_username = instagramUsername,
                    instagram_password = instagramPassword
                };
                IGAccount success = manager.ReloginSession(account, user, ref message);
                user.instagram_password = "Pass21234";
                Assert.AreEqual(success.accountId, account.accountId);
            }
        }
        
        /// Can't run this test with other test. This test is required to insert verify_code what sends to phone.
        public void SmsVerifySession()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                string verifyCode = "";
                User userCache = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(userCache.userId);
                account.accountUsername = account.accountUsername + "2";
                account.sessionSave = manager.Encrypt(File.ReadAllText(accountPath));
                account.State.stateRelogin = true;
                context.IGAccounts.Update(account);
                context.SaveChanges();
                bool success = manager.SmsVerifySession(account.accountId, userCache.userId, verifyCode, ref message);
                Assert.AreEqual(success, true);
            }
        }
    } 
}