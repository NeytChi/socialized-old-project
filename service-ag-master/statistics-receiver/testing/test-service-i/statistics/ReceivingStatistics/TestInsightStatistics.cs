using System;
using Serilog;
using System.Linq;
using Serilog.Core;
using NUnit.Framework;
using System.Collections.Generic;

using Controllers;
using database.context;
using Models.Statistics;
using InstagramService.Statistics;

namespace Testing.Service.Statistics
{
    [TestFixture]
    public class TestInsightStatistics
    {
        public Context context;
        public InsightStatistics receiver;
        public BusinessAccount account;
        public DateTime time = new DateTime(
            DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 0, 0);

        public TestInsightStatistics()
        {
            Logger log = new LoggerConfiguration().CreateLogger();
            context = TestMockingContext.GetContext();
            StatisticsService service = new StatisticsService(log);
            this.receiver = new InsightStatistics(service, context, new JsonHandler(log));
            account = TestMockingContext.BusinessAccountEnviroment();
        }
        [Test]
        public void InsightsPost()
        {
            PostStatistics post = CreatePostStatistics();
            receiver.InsightsPost(account);
        }
        [Test]
        public void GetURLPostInsights()
        {
            var success = receiver.GetURLPostInsights(
                TestMockingContext.values.ig_media_id, "IMAGE",
                TestMockingContext.values.access_token);
            Assert.AreEqual(success.Contains(TestMockingContext.values.ig_media_id), true);
        }
        [Test]
        public void UpdatePostInsights()
        {
            PostStatistics post = CreatePostStatistics();
            List<StatisticsObject> insights = new List<StatisticsObject>();
            for (int i = 0; i < 5; i++) {
                StatisticsObject insight = new StatisticsObject();
                insight.values = new List<StatisticsValue>();
                insight.values.Add(new StatisticsValue() {
                    value = 1,
                    end_time = time
                });
                insights.Add(insight);
            }
            receiver.UpdatePostInsights(insights, post);
        }
        [Test]
        public void InsightsStory()
        {
            StoryStatistics story = CreateStoryStatistics();
            receiver.InsightsStories(account);
        }
        [Test]
        public void UpdateStoryInsights()
        {
            StoryStatistics story = CreateStoryStatistics();
            List<StatisticsObject> insights = new List<StatisticsObject>();
            for (int i = 0; i < 4; i++) {
                StatisticsObject insight = new StatisticsObject();
                insight.values = new List<StatisticsValue>();
                insight.values.Add(new StatisticsValue() {
                    value = 1,
                    end_time = time
                });
                insights.Add(insight);
            }
            receiver.UpdateStoryInsights(insights, story);
        }
        [Test]
        public void GetURLStoryInsights()
        {
            var success = receiver.GetURLStoryInsights(
                TestMockingContext.values.ig_story_id, 
                TestMockingContext.values.access_token);
            Assert.AreEqual(success.Contains(TestMockingContext.values.ig_story_id), true);
        }
        public PostStatistics CreatePostStatistics()
        {
            var posts = context.PostStatistics.Where(p
                => p.accountId == account.businessId).ToList();
            context.PostStatistics.RemoveRange(posts);
            context.SaveChanges();
            PostStatistics post = new PostStatistics();
            post.accountId = account.businessId;
            post.IGMediaId = TestMockingContext.values.ig_media_id;
            context.PostStatistics.Add(post);
            context.SaveChanges();
            return post;   
        }
        public StoryStatistics CreateStoryStatistics()
        {
            var stories = context.StoryStatistics.Where(p
                => p.accountId == account.businessId).ToList();
            context.StoryStatistics.RemoveRange(stories);
            context.SaveChanges();
            StoryStatistics story = new StoryStatistics();
            story.accountId = account.businessId;
            story.mediaId = TestMockingContext.values.ig_story_id;
            context.StoryStatistics.Add(story);
            context.SaveChanges();
            return story;   
        }
    }
}