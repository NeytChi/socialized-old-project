using Serilog;
using NUnit.Framework;
using database.context;
using InstagramService;
using Instasoft.Testing;
using Models.GettingSubscribes;
using ngettingsubscribers;
using Managment;
using InstagramApiSharp.API;

namespace Common
{
    [TestFixture]
    public class TestUnfollowingGS
    { 
        public Context context;
        public UnfollowingGS unfollowing;
        public TaskBranch branch;
        public long firstUserPk = 19330388085;  /// user - nikachikapika2 
        public long secondUserPk = 21363493760; /// user - nikitatesterchi 

        public TestUnfollowingGS()
        {
            this.context = new Context(true, true);
            SessionStateHandler handler = new SessionStateHandler(context);
            unfollowing = new UnfollowingGS(OptionsGS.GetInstance(handler),new LoggerConfiguration().CreateLogger(),handler);
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
            InstagramApi.GetInstance().users.FollowUser(ref branch.session, secondUserPk);
            branch.currentUnit.userPk = firstUserPk;
            bool success = unfollowing.HandleTask(context, ref branch);
            bool unsuccess = unfollowing.HandleTask(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckOptions()
        {
            branch.currentTask.taskOption.likeUsersPost = true;
            branch.currentUnit.userPk = branch.session.User.LoggedInUser.Pk;
            bool success = unfollowing.CheckOptions(context, ref branch);
            var media = unfollowing.options.GetMedia(ref branch.session, branch.currentUnit.userPk, 0);
            InstagramApi.GetInstance().media.UnLikeMediaAsync(ref branch.session, media[0].Pk);
            bool unsuccess = unfollowing.CheckOptions(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }  
        [Test]
        public void UnfollowUser()
        {
            InstagramApi.GetInstance().users.FollowUser(ref branch.session, secondUserPk);
            branch.currentUnit.userPk = secondUserPk;
            bool success = unfollowing.UnfollowUser(context, ref branch);
            bool unsuccess = unfollowing.UnfollowUser(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}