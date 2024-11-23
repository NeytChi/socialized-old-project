using Serilog;
using NUnit.Framework;
using database.context;
using InstagramService;
using Instasoft.Testing;
using Models.GettingSubscribes;
using ngettingsubscribers;
using Managment;

namespace Common
{
    [TestFixture]
    public class TestFollowingGS
    { 
        public Context context;
        public FollowingGS followingGS;
        public TaskBranch branch;
        private long userPk = 8273287;              
        /// get user_pk      @laurensimpson -> 8273287    nyknicks -> 2122607
        
        public TestFollowingGS()
        {
            context = new Context(true, true);
            SessionStateHandler handler = new SessionStateHandler(context);
            followingGS = new FollowingGS(OptionsGS.GetInstance(handler),new LoggerConfiguration().CreateLogger(),handler);
            TestMockingContext.context = context;
            UnitGS unit = TestMockingContext.CreateUnitGSEnviroment();
            branch = unit.Data.Task.Branch;
            branch.currentTask = unit.Data.Task;
            branch.currentTaskData = unit.Data;
            branch.currentUnit = unit;
            branch.session = SessionManager.CreateInstance(context).LoadSession(branch.sessionId);
        }
        [Test]
        public void HandleTask()
        {
            branch.currentUnit = new UnitGS();
            branch.currentUnit.userPk = userPk;
            branch.currentTask.taskOption.likeUsersPost = false;
            branch.currentTask.taskOption.autoUnfollow = true;
            branch.currentTask.taskOption.watchStories = false;
            bool success = followingGS.HandleTask(context,ref branch);
            bool unsuccess = followingGS.HandleTask(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void FollowUser()
        {
            branch.currentUnit = new UnitGS();
            branch.currentUnit.userPk = userPk;
            branch.currentTask.taskOption.likeUsersPost = false;
            branch.currentTask.taskOption.autoUnfollow = true;
            branch.currentTask.taskOption.watchStories = false;
            bool success = followingGS.FollowUser(context,ref branch);
            bool unsuccess = followingGS.FollowUser(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckOptions()
        {
            branch.currentUnit = new UnitGS();
            branch.currentUnit.userPk = userPk;
            branch.currentTask.taskOption.autoUnfollow = false;
            bool success = followingGS.CheckOptions(context,ref branch);
            bool unsuccess = followingGS.CheckOptions(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void OptionAutoUnfollow()
        {
            branch.currentUnit = new UnitGS();
            branch.currentUnit.userPk = userPk;
            branch.currentTask.taskOption.autoUnfollow = false;
            bool success = followingGS.OptionAutoUnfollow(context, ref branch);
            bool unsuccess = followingGS.OptionAutoUnfollow(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void OptionWatchStories()
        {
            branch.currentUnit = new UnitGS();
            branch.currentUnit.userPk = userPk;
            branch.currentTask.taskOption.watchStories = true;
            bool success = followingGS.OptionWatchStories(context,ref branch);
            bool unsuccess = followingGS.OptionWatchStories(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void OptionLikesUser()
        {
            branch.currentUnit = new UnitGS();
            branch.currentUnit.userPk = userPk;
            branch.currentTask.taskOption.likeUsersPost = true;
            bool success = followingGS.OptionLikesUser(context,ref branch);
            bool unsuccess = followingGS.OptionLikesUser(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}
