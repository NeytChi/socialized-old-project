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
    public class TestPostingStatistics
    {
        public Context context;
        public PostingStatistics receiver;
        public BusinessAccount account;
        public DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 0, 0);
        StatisticsService service;

        public TestPostingStatistics()
        {
            Logger log = new LoggerConfiguration().CreateLogger();
            context = TestMockingContext.GetContext();
            this.service = new StatisticsService(log);
            this.receiver = new PostingStatistics(service, context, new JsonHandler(log));
            account = TestMockingContext.BusinessAccountEnviroment();
        }
        [Test]
        public void PostStatistics()
        {
            if (TestMockingContext.values.send_request_to_instagram_service)
                receiver.GetStatistics(account);
        }
        [Test]
        public void PostStatistics_From_Day()
        {
            PostingStatistics posting = new PostingStatistics(service, context, new JsonHandler(
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
        public void ReceivePosts()
        {
            JObject json = JsonConvert.DeserializeObject<JObject>(
            "{\n" +
                "\"data\" : \n" +
                "[\n" +
                    "{\n" +
                        "comments: \n" +
                        "{\n" +
                            "\"data\" : \n" +
                            "[\n" +
                                "{\n" +
                                    "\"timestamp\" : \"" + time.ToString() + "\"\n" + 
                                "}\n" +
                            "]\n" + 
                        "}," + 
                        "\"like_count\": 66,\n" +
                        "\"media_url\": \"Test URL to image\",\n" +
                        "\"comments_count\": 1,\n" +
                        "\"timestamp\": \"2018-05-28T04:58:00+0000\",\n" +
                        "\"id\": \"17915032921174916\"\n" +
                    "}\n" +
                "]\n" +
            "}"                 
            );
            receiver.ReceivePosts(json, account.businessId);
        }
        [Test]
        public void SavePosts()
        {
            List<PostValues> posts = new List<PostValues>();
            PostValues values = new PostValues()
            {
                like_count = 1,
                media_url = "url",
                comments_count = 1,
                timestamp = time,
                comments = JsonConvert.DeserializeObject<JObject>(
                "{\n" +
                    "\"data\" : \n" +
                    "[\n" +
                        "{\n" +
                            "\"timestamp\" : \"" + time.ToString() + "\"\n" + 
                        "}\n" +
                    "]\n" + 
                "}")
            };
            posts.Add(values);
            receiver.SavePosts(posts, account.businessId);
        }
        [Test]
        public void SaveCommentsStatistics()
        {
            PostStatistics post = new PostStatistics();
            post.accountId = account.businessId;
            context.PostStatistics.Add(post);
            context.SaveChanges();
            JObject json = JsonConvert.DeserializeObject<JObject>(
            "{\n \"data\" : \n [\n" +
                    "{\n \"timestamp\" : \"" + time.ToString() + "\"\n }\n ]\n }");
            var success = receiver.SaveCommentsStatistics(json, post.postId);
            Assert.AreEqual(success.Count, 1);
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