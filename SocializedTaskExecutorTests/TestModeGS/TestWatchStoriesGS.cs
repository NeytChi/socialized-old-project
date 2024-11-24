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
    public class TestWatchStoriesGS
    {
        public Context context;
        public WatchStoriesGS watchStoriesGS;
        public TaskBranch branch;
        public long firstUserPk = 19330388085;  /// user - nikachikapika2 
        public long secondUserPk = 21363493760; /// user - nikitatesterchi 

        public TestWatchStoriesGS()
        {
            context = new Context(true, true);
            SessionStateHandler handler = new SessionStateHandler(context);
            watchStoriesGS = new WatchStoriesGS(OptionsGS.GetInstance(handler),new LoggerConfiguration().CreateLogger(),handler);
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
            branch.currentUnit.userPk = firstUserPk;
            bool success = watchStoriesGS.HandleTask(context, ref branch);
            bool unsuccess = watchStoriesGS.HandleTask(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        } 
        [Test]
        public void WatchStoriesUsers()
        {
            branch.currentUnit.userPk = firstUserPk;
            bool success = watchStoriesGS.WatchStoriesUsers(context, ref branch);
            bool unsuccess = watchStoriesGS.WatchStoriesUsers(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        } 
    }
}