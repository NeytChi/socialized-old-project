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

namespace Tests.InstagramAccountStatistics.Statistics
{
    [TestFixture]
    public class TestStoriesStatistics
    {
        public Context context;
        public StoriesStatistics receiver;
        public BusinessAccount account;
        public DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 0, 0);
        StatisticsService service;

        public TestStoriesStatistics()
        {
            Logger log = new LoggerConfiguration().CreateLogger();
            this.context = TestMockingContext.GetContext();
            this.context = new Context(false);
            this.service = new StatisticsService(log);
            this.receiver = new StoriesStatistics(service, context, new JsonHandler(log));
            account = TestMockingContext.BusinessAccountEnviroment();
        }
        [Test]
        public void Receive()
        {
            if (TestMockingContext.values.send_request_to_instagram_service)
                receiver.GetStatistics(account);
        }
        [Test]
        public void Receive_From_Day()
        {
            StoriesStatistics posting = new StoriesStatistics(service, context, new JsonHandler(
                new LoggerConfiguration().CreateLogger()), 3);
            if (TestMockingContext.values.send_request_to_instagram_service)
                posting.GetStatistics(account);
        }
        [Test]
        public void GetURL()
        {
            var success = receiver.GetURL(
                TestMockingContext.values.business_account_id, 
                TestMockingContext.values.access_token);
            Assert.AreEqual(string.IsNullOrEmpty(success), false);
        }
        [Test]
        public void ReceiveStories()
        {
            JObject json = JsonConvert.DeserializeObject<JObject>(
            "{\n" +
                "\"data\" : \n" +
                "[" +
                    "\n {\n \"media_url\": \"https://scontent.xx.fbcdn.net/v/t51.12442-15/91155916_2569626006609086_4262385964961320278_n.jpg?_nc_cat=111&_nc_sid=8ae9d6&_nc_ohc=DKJOMDFU754AX8zRVB0&_nc_ht=scontent.xx&oh=03e8b91c607af3db1888198a00b72a06&oe=5EA79D20\"," +
                    "\n \"timestamp\": \"2020-03-30T07:14:34+0000\"," +
                    "\n \"id\": \"18057170347212162\"," +
                    "\n \"media_type\": \"IMAGE\"\n }\n" +
                "]" +
            "}"                 
            );
            receiver.ReceiveStories(json, account.businessId);
        }
        [Test]
        public void SaveStories()
        {
            List<StoryValues> stories = new List<StoryValues>();
            stories.Add( new StoryValues() {
                id = "1",
                media_url = "url",
                media_type = "IMAGE",
                timestamp = time
            });
            receiver.SaveStories(stories, account.businessId);
        }
        [Test]
        public void CheckTokenAndIG()
        {
            bool success = receiver.CheckTokenAndIG(
                TestMockingContext.values.access_token, 
                TestMockingContext.values.business_account_id);
            bool unsuccess = receiver.CheckTokenAndIG(TestMockingContext.values.access_token, null);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}