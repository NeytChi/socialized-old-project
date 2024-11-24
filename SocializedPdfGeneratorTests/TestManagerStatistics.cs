using Serilog;
using NUnit.Framework;

using database.context;
using Models.Common;
using Models.Statistics;
using Models.SessionComponents;
using InstagramService.Statistics;

namespace Testing.Service
{
    [TestFixture]
    public class TestManagerStatistics
    {
        public Context context;
        public ManagerStatistics manager;
        public string message;
        
        public TestManagerStatistics()
        {
            context = TestMockingContext.GetContext();
            this.manager = new ManagerStatistics(new LoggerConfiguration().CreateLogger(), context);
        }
        [Test]
        public void CreateBusinessAccount()
        {
            User user = TestMockingContext.CreateUser();
            IGAccount account = TestMockingContext.CreateSession(user.userId);
            // if (TestMockingContext.values.send_request_to_instagram_service) {
            //     var success = manager.CreateBusinessAccount(
            //         TestMockingContext.values.access_token, user, ref message);
            //     Assert.AreEqual(success.userId, user.userId);
            // }
        }
        [Test]
        public void CreateExistAccountBusinessAccount()
        {
            // User user = TestMockingContext.CreateUser();
            // var account = TestMockingContext.CreateBusinessAccount(user);
            // if (TestMockingContext.values.send_request_to_instagram_service) {
            //     var result = manager.CreateBusinessAccount(
            //         TestMockingContext.values.access_token, user, ref message);
            //     Assert.AreEqual(result, null);
            // }
        }
        [Test]
        public void RemoveAccount()
        {
            BusinessAccount account = TestMockingContext.BusinessAccountEnviroment();
            bool success = manager.RemoveAccount(account.user.userToken, account.businessId, ref message);
            bool unsuccess = manager.RemoveAccount(account.user.userToken, account.businessId, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}