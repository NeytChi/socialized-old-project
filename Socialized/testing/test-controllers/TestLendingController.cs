using Controllers;
using System.Linq;
using Models.Lending;
using NUnit.Framework;
using database.context;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestLendingController
    {
        
        public TestLendingController()
        {
            this.context = TestMockingContext.GetContext();
            this.controller = new LendingController(context);
            this.email = TestMockingContext.values.follower_email;
        }
        LendingController controller;
        public Context context;
        public string email;
        string message;
        
        [Test]
        public void FollowTo()
        {
            FollowerCache cache = GetFollowerCache();
            var result = controller.FollowTo(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void Follow_To_With_Wrong_Email_Format()
        {
            FollowerCache cache = GetFollowerCache();
            cache.follower_email = "123";
            var result = controller.FollowTo(cache);
            Assert.AreEqual(result.Value.success, false);
        }
        [Test]
        public void Follow_To_With_Exist_User()
        {
            FollowerCache cache = GetFollowerCache();
            TestMockingContext.CreateUser(cache.follower_email);
            var result = controller.FollowTo(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        public FollowerCache GetFollowerCache()
        {
            Follower nfollower = context.Followers.Where(f => f.followerEmail == email).FirstOrDefault();
            if (nfollower != null)
                context.Followers.Remove(nfollower);
            context.SaveChanges();
            FollowerCache cache = new FollowerCache();
            cache.follower_email = email;
            return cache;
        }
        [Test]
        public void GetFollowerByEmail()
        {
            Follower nfollower = context.Followers.Where(f => f.followerEmail == email).FirstOrDefault();
            if (nfollower != null)
                context.Followers.Remove(nfollower);
            context.SaveChanges();
            nfollower = controller.GetFollowerByEmail(email, ref message);
            controller.AddFollower(email, 0);
            Follower follower = controller.GetFollowerByEmail(email, ref message);
            Assert.AreEqual(follower.followerEmail, email);
            Assert.AreEqual(nfollower, null);
        }
        [Test]
        public void AddFollower()
        {
            controller.AddFollower(email, 0);
            Follower nfollower = context.Followers.Where(f => f.followerEmail == email).FirstOrDefault();
            if (nfollower != null)
                context.Followers.Remove(nfollower);
            context.SaveChanges();
        }
    }    
}