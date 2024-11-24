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
    public class TestLikingGS
    { 
        public Context context;
        public LikingGS likingGS;
        public TaskBranch branch;

        public long userPk = 2122607;              /// get user_pk      
        // @laurensimpson -> 8273287    nyknicks -> 2122607
        public TestLikingGS()
        {
            context = new Context(true,true);
            SessionStateHandler handler = new SessionStateHandler(context);
            likingGS = new LikingGS(OptionsGS.GetInstance(handler),new LoggerConfiguration().CreateLogger(),handler);
            TestMockingContext.context = context;
            UnitGS unit = TestMockingContext.CreateUnitGSEnviroment();
            unit.userPk = userPk;
            unit.username = "laurensimpson";
            branch = unit.Data.Task.Branch;
            branch.currentTask = unit.Data.Task;
            branch.currentTaskData = unit.Data;
            branch.currentUnit = unit;
            branch.session = SessionManager.CreateInstance(context).LoadSession(branch.sessionId);
        }
        [Test]
        public void HandleTask()
        {
            branch.currentTask.taskOption.watchStories = false;
            branch.currentTask.taskOption.likesOnUser = 1;
            bool success = likingGS.HandleTask(context,ref branch);
            var media = likingGS.options.GetMedia(ref branch.session, branch.currentUnit.userPk, 0);
            InstagramApi.GetInstance().media.UnLikeMediaAsync(ref branch.session, media[0].Pk);
            bool unsuccess = likingGS.HandleTask(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckOptions()
        {
            branch.currentTask.taskOption.watchStories = false;
            branch.currentTask.taskOption.likesOnUser = 1;
            bool success = likingGS.CheckOptions(context,ref branch);
            var media = likingGS.options.GetMedia(ref branch.session, branch.currentUnit.userPk, 0);
            InstagramApi.GetInstance().media.UnLikeMediaAsync(ref branch.session, media[0].Pk);
            bool unsuccess = likingGS.CheckOptions(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void OptionLikesUser()
        {
            branch.currentTask.taskOption.likesOnUser = 1;
            bool success = likingGS.OptionLikesUser(context,ref branch);
            var media = likingGS.options.GetMedia(ref branch.session, branch.currentUnit.userPk, 0);
            InstagramApi.GetInstance().media.UnLikeMediaAsync(ref branch.session, media[0].Pk);
            bool unsuccess = likingGS.OptionLikesUser(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void OptionWatchStories()
        {
            branch.currentTask.taskOption.watchStories = true;
            bool success = likingGS.OptionWatchStories(context,ref branch);
            bool unsuccess = likingGS.OptionWatchStories(null,ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}