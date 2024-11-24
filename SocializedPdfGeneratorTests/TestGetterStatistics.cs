using System;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;

using database.context;
using Models.Statistics;
using InstagramService.Statistics;

namespace Testing.Service
{
    [TestFixture]
    public class TestGetterStatistics
    {
        public Context context;
        public GetterStatistics getter;
        public BusinessAccount account;
        public string message;
        
        public TestGetterStatistics()
        {
            context = TestMockingContext.GetContext();
            this.getter = new GetterStatistics(context);
            account = TestMockingContext.BusinessAccountEnviroment();
        }
        [Test]
        public void SelectByTime()
        {
            var success = getter.SelectByTime(account, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(-1));      
        }
        [Test]
        public void GetDayStatistics()
        {
            var day = CreateDayStatistics();
            var success = getter.GetDayStatistics(account.businessId, day.endTime.AddDays(-1), day.endTime.AddDays(2));
            var unsuccess = getter.GetDayStatistics(0, day.endTime, day.endTime);
            Assert.AreEqual(success.Count(), 1);
            Assert.AreEqual(unsuccess.Count(), 0);
        }
        [Test]
        public void GetOnlineFollowers()
        {
            var online = CreateOnlineFollowers();
            var success = getter.GetOnlineFollowers(account.businessId, online.endTime.AddDays(2));
            var unsuccess = getter.GetDayStatistics(0, online.endTime, online.endTime);
            Assert.AreEqual(success.Count(), 1);
            Assert.AreEqual(unsuccess.Count(), 0);
        }
        [Test]
        public void GetPostStatistics()
        {
            var post = CreatePost();
            var success = getter.GetPostStatistics(account.businessId, post.timestamp.AddDays(-1), post.timestamp.AddDays(2));
            var unsuccess = getter.GetDayStatistics(0, post.timestamp, post.timestamp);
            Assert.AreEqual(success.Count(), 1);
            Assert.AreEqual(unsuccess.Count(), 0);
        }
        [Test]
        public void General()
        {
            dynamic success = getter.General(account, new List<DayStatistics>(), new List<PostStatistics>(), 1);
            Assert.AreEqual(success.followers.followers_count, account.followersCount);
        }
        [Test]
        public void GetProfileActivity()
        {
            var day = CreateDayStatistics();
            day.profileViews = 1;
            List<DayStatistics> days = new List<DayStatistics>();
            days.Add(day);
            dynamic success = getter.GetProfileActivity(days);
            Assert.AreEqual(success.profile_views.value, day.profileViews);
        }
        [Test]
        public void CountCoefficient()
        {
            double success = getter.CountCoefficient(15, 20);
            double withZero = getter.CountCoefficient(0, 20);
            Assert.AreEqual((int)success, 133);
            Assert.AreEqual((int)withZero, 2000);
        }
        [Test]
        public void SelectPostHistory()
        {
            var post = CreatePost();
            List<PostStatistics> posts = new List<PostStatistics>();
            posts.Add(post);
            var success = getter.SelectPostHistory(posts);
            Assert.AreEqual(success.Count, 1);
        }
        [Test]
        public void GetPostHistory()
        {
            Dictionary<DateTime, int> history = new Dictionary<DateTime, int>();
            history.Add(DateTime.Now, 1);
            dynamic success = getter.GetPostHistory(history);
            Assert.AreEqual(success.Count, 1);
        }
        [Test]
        public void GrowFollowers()
        {
            var day = CreateDayStatistics();
            day.profileViews = 1;
            List<DayStatistics> days = new List<DayStatistics>();
            days.Add(day);
            List<dynamic> success = getter.GrowFollowers(days, 50);
            Assert.AreEqual(success.Count, 1);
        }
        [Test]
        public void GetPostActivity()
        {
            var post = CreatePost();
            post.engagement = 1;
            post.saved = 1;
            List<PostStatistics> posts = new List<PostStatistics>();
            posts.Add(post);
            var success = getter.GetPostActivity(posts, posts, 1);
            Assert.AreEqual(success.post_engagement.value, 1);
        }
        [Test]
        public void Posts()
        {
            PostStatistics post = CreatePost();
            List<PostStatistics> posts = new List<PostStatistics>();
            List<PostStatistics> last = new List<PostStatistics>();
            posts.Add(post);
            dynamic success = getter.ColumnGraphic(last.Count, posts.Count,
                post.timestamp.AddDays(-1), post.timestamp, post.timestamp.AddDays(1));
            Assert.AreEqual(success.last.value, 0);
            Assert.AreEqual(success.current.value, 1);
        }
        [Test]
        public void GetColorOnlineFollowers()
        {
            long followersCount = 100;
            Assert.AreEqual(getter.GetColorOnlineFollowers(followersCount, 12), 1);
            Assert.AreEqual(getter.GetColorOnlineFollowers(followersCount, 32), 2);
            Assert.AreEqual(getter.GetColorOnlineFollowers(followersCount, 52), 3);
            Assert.AreEqual(getter.GetColorOnlineFollowers(followersCount, 72), 4);
            Assert.AreEqual(getter.GetColorOnlineFollowers(followersCount, 92), 5);
            Assert.AreEqual(getter.GetColorOnlineFollowers(followersCount, 112), 5);
            Assert.AreEqual(getter.GetColorOnlineFollowers(followersCount, -5), 1);
        }
        /// <summary>
        /// Create mocking DayStatistics in database context for testing.
        /// </summary>
        public DayStatistics CreateDayStatistics()
        {
            var days = context.Statistics.Where(s => s.accountId == account.businessId).ToList();
            context.Statistics.RemoveRange(days);
            DayStatistics day = new DayStatistics()
            {
                accountId = account.businessId,
                Account = account,
                endTime = DateTime.Now
            };
            context.Statistics.Add(day);
            context.SaveChanges();
            return day;
        }
        public OnlineFollowers CreateOnlineFollowers()
        {
            var followers = context.OnlineFollowers.Where(s => s.accountId == account.businessId).ToList();
            context.OnlineFollowers.RemoveRange(followers);
            OnlineFollowers online = new OnlineFollowers()
            {
                accountId = account.businessId,
                Account = account,
                endTime = DateTime.Now
            };
            context.OnlineFollowers.Add(online);
            context.SaveChanges();
            return online;
        } 
        public PostStatistics CreatePost()
        {
            var posts = context.PostStatistics.Where(s => s.accountId == account.businessId).ToList();
            context.PostStatistics.RemoveRange(posts);
            PostStatistics post = new PostStatistics()
            {
                accountId = account.businessId,
                Account = account,
                timestamp = DateTime.Now
            };
            context.PostStatistics.Add(post);
            context.SaveChanges();
            return post;
        }
    }
}