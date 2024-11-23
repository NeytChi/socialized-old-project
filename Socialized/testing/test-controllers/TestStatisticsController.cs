using System;
using NUnit.Framework;

using Controllers;
using database.context;
using Models.Common;
using Models.Statistics;
using Models.SessionComponents;


namespace Testing.Controllers
{
    [TestFixture]
    public class TestStatisticsController
    { 
        public TestStatisticsController()
        {
            context = TestMockingContext.GetContext();
            controller = new StatisticsController(context);
            controller.manager.context = context;
            controller.getter.context = context;
            User user = TestMockingContext.CreateUser();
            account = TestMockingContext.CreateSession(user.userId);
            businessAccount = TestMockingContext.CreateBusinessAccount(account);
            account.User = user;
            // businessAccount.igAccount = account;
        }
        public Context context;
        public StatisticsController controller;
        public BusinessAccount businessAccount;
        public IGAccount account;

        [Test]
        public void Start()
        {
            StatisticsCache cache = new StatisticsCache();
            cache.access_token = TestMockingContext.values.access_token;
            cache.user_token = account.User.userToken;
            // var success = controller.Start(cache);
            // cache.user_token = "";
            // var unsuccess = controller.Start(cache);
            // Assert.AreEqual(success.Value.success, true);
            // Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void ByTime()
        {
            StatisticsCache cache = new StatisticsCache();
            cache.user_token = account.User.userToken;
            cache.account_id = account.accountId;
            cache.from = DateTime.Now.AddDays(7);
            cache.to = DateTime.Now;
            var success = controller.ByTime(cache);
            cache.account_id = 0;
            var unsuccess = controller.ByTime(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void Remove()
        {
            StatisticsCache cache = new StatisticsCache();
            cache.user_token = account.User.userToken;
            cache.account_id = account.accountId;
            var success = controller.Remove(cache);
            cache.account_id = 0;
            var unsuccess = controller.Remove(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
    }
}