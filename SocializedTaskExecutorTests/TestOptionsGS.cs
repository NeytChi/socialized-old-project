using Models.Common;
using NUnit.Framework;
using database.context;
using InstagramService;
using Instasoft.Testing;
using Models.SessionComponents;
using InstagramApiSharp.API.Builder;
using ngettingsubscribers;
using InstagramApiSharp.API;
using Managment;

namespace Common.Testing
{
    [TestFixture]
    public class TestOptionsGS
    { 
        public TestOptionsGS()
        {
            this.context = new Context(true, true);
            TestMockingContext.context = context;
            UserCache user = TestMockingContext.CreateUser();
            SessionCache sessionCache = TestMockingContext.CreateSession(user.userId);
            session = SessionManager.CreateInstance(context).LoadSession(sessionCache.sessionId);
            optionsGS = OptionsGS.GetInstance(new SessionStateHandler(context));
        }
        public Context context;
        private OptionsGS optionsGS;
        private long userPk = 8273287;
        public Session session;
        public InstagramApi api = InstagramApi.GetInstance();
        [Test]
        public void LikeUsersPost()
        {
            Assert.AreEqual(optionsGS.LikeUsersPost(ref session, true, userPk), true);
            Assert.AreEqual(optionsGS.LikeUsersPost(ref session, false, userPk), true);
        }
        [Test]
        public void WatchStories()
        {
            Assert.AreEqual(optionsGS.WatchStories(ref session, true, userPk), true);
            Assert.AreEqual(optionsGS.WatchStories(ref session, false, userPk), true);
        }
        [Test]
        public void DontFollowOnPrivate()
        {
            Assert.AreEqual(optionsGS.DontFollowOnPrivate(true, false), true);
            Assert.AreEqual(optionsGS.DontFollowOnPrivate(true, true), false);
        }
        [Test]
        public void AutoUnfollow()
        {
            api.users.FollowUser(ref session, userPk);
            bool success = optionsGS.AutoUnfollow(ref session, true, userPk);
            api.users.FollowUser(ref session, userPk);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void GetAccessUnfollowNonReciprocal()
        {
            api.users.FollowUser(ref session, userPk);
            bool success = optionsGS.GetAccessUnfollowNonReciprocal(ref session, true, userPk);
            optionsGS.AutoUnfollow(ref session, true, userPk);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void NextUnlocking()
        {
            api.users.BlockUser(ref session, userPk);
            Assert.AreEqual(optionsGS.NextUnlocking(ref session, true, userPk), true);
            Assert.AreEqual(optionsGS.NextUnlocking(ref session, false, userPk), true);
        }
    }
}