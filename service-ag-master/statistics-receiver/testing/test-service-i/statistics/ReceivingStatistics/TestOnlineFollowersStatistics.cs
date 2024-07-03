using System;
using Serilog;
using Serilog.Core;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using Controllers;
using database.context;
using Models.Statistics;
using InstagramService.Statistics;

namespace Testing.Service.Statistics
{
    [TestFixture]
    public class TestOnlineFollowersStatistics
    {
        public Context context;
        public OnlineFollowersStatistics receiver;
        public BusinessAccount account;
        public DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 0, 0);
        StatisticsService service;
        public TestOnlineFollowersStatistics()
        {
            Logger log = new LoggerConfiguration().CreateLogger();
            context = TestMockingContext.GetContext();
            this.service = new StatisticsService(log);
            this.receiver = new OnlineFollowersStatistics(service, context, new JsonHandler(log));
            account = TestMockingContext.BusinessAccountEnviroment();
        }
        [Test]
        public void OnlineFollowers()
        {
            if (TestMockingContext.values.send_request_to_instagram_service)
                receiver.GetStatistics(account);
        }
        [Test]
        public void OnlineFollowers_With_Days()
        {
            var online = new OnlineFollowersStatistics(service, context, new JsonHandler(
                new LoggerConfiguration().CreateLogger()), 3);
            if (TestMockingContext.values.send_request_to_instagram_service)
                online.GetStatistics(account);
        }
        [Test]
        public void GetStartUrl()
        {
            var success = receiver.GetStartUrl(
                TestMockingContext.values.business_account_id, 
                TestMockingContext.values.access_token);
            Assert.AreEqual(string.IsNullOrEmpty(success), false);
        }
        [Test]
        public void SaveOnlineFollowers()
        {
            StatisticsOnlineFollowers followers = new StatisticsOnlineFollowers();
            followers.values = new List<OnlineFollowersValues>();
            JObject onlineValues = JsonConvert.DeserializeObject<JObject>(
                "{" +
                    "\"0\": 16, \"1\": 14, \"2\": 15, \"3\": 16, \"4\": 14, \"5\": 12, " +
                    "\"6\": 17, \"7\": 18, \"8\": 17, \"9\": 16, \"10\": 11, \"11\": 16, " +
                    "\"12\": 12, \"13\": 13, \"14\": 9, \"15\": 9, \"16\": 6, \"17\": 7, " +
                    "\"18\": 6, \"19\": 9, \"20\": 6, \"21\": 6, \"22\": 11, \"23\": 10 " +
                "}"
            );
            JObject emptyValues = JsonConvert.DeserializeObject<JObject>("{}");
            var onlineFollowersValues = new OnlineFollowersValues()
            {
                value = onlineValues,
                end_time = time
            };
            var emptyFollowersValues = new OnlineFollowersValues()
            {
                value = emptyValues,
                end_time = time
            };
            followers.values.Add(onlineFollowersValues);
            followers.values.Add(emptyFollowersValues);
            receiver.SaveOnlineFollowers(followers, account.businessId);
        }
    }
}