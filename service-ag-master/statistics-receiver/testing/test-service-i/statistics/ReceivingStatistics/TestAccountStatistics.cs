using System;
using Serilog;
using Serilog.Core;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Controllers;
using database.context;
using Models.Statistics;
using InstagramService.Statistics;

namespace Testing.Service.Statistics
{
    [TestFixture]
    public class TestAccountStatistics
    {
        public Context context;
        public AccountStatistics receiver;
        public BusinessAccount account;
        public DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 0, 0);

        public TestAccountStatistics()
        {
            Logger log = new LoggerConfiguration().CreateLogger();
            context = TestMockingContext.GetContext();
            StatisticsService service = new StatisticsService(log);
            this.receiver = new AccountStatistics(service, context, new JsonHandler(log));
            account = TestMockingContext.BusinessAccountEnviroment();
        }
        [Test]
        public void UpdateBusinessAccount()
        {
            if (TestMockingContext.values.send_request_to_instagram_service)
                receiver.UpdateBusinessAccount(ref account);
        }
        [Test]
        public void ReceiveAccountInfo()
        {
            JObject followersCount = JsonConvert.DeserializeObject<JObject>(
                "{" +
                    "\"followers_count\": 16 " +
                "}"
            );
            receiver.ReceiveAccountInfo(followersCount, ref account);
        }
        [Test]
        public void GetURLAccount()
        {
            var success = receiver.GetURLAccount(TestMockingContext.values.business_account_id, TestMockingContext.values.access_token);
            Assert.AreEqual(success[0], TestMockingContext.values.business_account_id[0]);
        }
    }
}
