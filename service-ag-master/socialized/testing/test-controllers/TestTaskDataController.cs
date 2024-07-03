
using Managment;
using Controllers;
using NUnit.Framework;
using database.context;
using Models.GettingSubscribes;
using Microsoft.AspNetCore.Mvc;


namespace Testing.Controllers
{
    [TestFixture]
    public class TestTaskDataController
    {
        public SessionManager manager;
        public TestTaskDataController()
        {
            context = TestMockingContext.GetContext();
            manager = new SessionManager(context);
            controller = new TaskDataController(context);
        }
        private Context context;
        private TaskDataController controller;
        
        [Test]
        public void Create()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskCache cache = new TaskCache()
            {
                user_token = TestMockingContext.values.user_token,
                task_id = data.taskId,
                username = "therock"
            };
            ActionResult<dynamic> success = controller.Create(cache);
            cache.user_token = null;
            ActionResult<dynamic> unsuccess = controller.Create(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void Delete()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskCache cache = new TaskCache()
            {
                user_token = TestMockingContext.values.user_token,
                data_id = data.dataId
            };
            ActionResult<dynamic> success = controller.Delete(cache);
            cache.user_token = null;
            ActionResult<dynamic> unsuccess = controller.Delete(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void StartStop()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskCache cache = new TaskCache()
            {
                user_token = TestMockingContext.values.user_token,
                data_id = data.dataId
            };
            ActionResult<dynamic> success = controller.StartStop(cache);
            cache.user_token = null;
            ActionResult<dynamic> unsuccess = controller.StartStop(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
    }
}