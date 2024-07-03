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
    public class TestBlockingGS
    {
        public Context context;
        public BlockingGS blockingGS;
        public TaskBranch branch = null;
        
        public TestBlockingGS()
        {
            context = new Context(true, true);
            SessionStateHandler handler = new SessionStateHandler(context);
            blockingGS = new BlockingGS(OptionsGS.GetInstance(handler),new LoggerConfiguration().CreateLogger(),handler);
            TestMockingContext.context = context;
            UnitGS unit = TestMockingContext.CreateUnitGSEnviroment();               
            branch = unit.Data.Task.Branch;
            branch.currentTask = unit.Data.Task;
            branch.currentTaskData = unit.Data;
            branch.currentUnit = unit;
            branch.session = SessionManager.CreateInstance(context).LoadSession(branch.sessionId);
        }
        public long firstUserPk = 19330388085;  /// user - nikachikapika2 
        public long secondUserPk = 21363493760; /// user - nikitatesterchi
        [Test]
        public void HandleTask()
        {
            branch.currentTask.taskOption.nextUnlocking = true;
            branch.currentUnit.userPk = firstUserPk;
            bool success = blockingGS.HandleTask(context, ref branch);
            bool unsuccess = blockingGS.HandleTask(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckOptions()
        {
            branch.currentUnit.userPk = secondUserPk;
            branch.currentTask.taskOption.nextUnlocking = false;
            blockingGS.HandleTask(context, ref branch);
            branch.currentTask.taskOption.nextUnlocking = true;
            bool success = blockingGS.CheckOptions(context, ref branch);
            bool unsuccess = blockingGS.CheckOptions(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}