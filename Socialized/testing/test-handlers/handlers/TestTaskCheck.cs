using Serilog;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

using Controllers;
using database.context;
using Models.Common;
using Models.GettingSubscribes;
using Models.SessionComponents;

namespace Testing.Handlers
{
    [TestFixture]
    public class TestTaskCheck
    {
        public TestTaskCheck()
        {
            context = TestMockingContext.GetContext();
            handler = new TaskCheck(new LoggerConfiguration().CreateLogger(), context);
        }
        public Context context;
        public TaskCheck handler;
        public string message = "";
        [Test]
        public void handle()
        {
            JObject json = new JObject();
            User user = TestMockingContext.CreateUser();
            IGAccount account = TestMockingContext.CreateSession(user.userId);
            TaskGS task = new TaskGS();
            task.taskType = 1;
            task.taskSubtype = 1;
            task.sessionId = account.accountId;
            bool success = handler.handle(ref json, ref task, ref message);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void GetNonDeleteTask()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            var success = handler.GetNonDeleteTask(task.taskType, task.taskSubtype, task.sessionId);
            var unsuccess = handler.GetNonDeleteTask(0, 0, task.sessionId);
            Assert.AreEqual(success.taskId, task.taskId);
            Assert.AreEqual(unsuccess, null);
        }
    }
}