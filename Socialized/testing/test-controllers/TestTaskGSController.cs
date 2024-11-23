using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;

using Managment;
using Controllers;
using database.context;
using Models.Common;
using Models.SessionComponents;
using Models.GettingSubscribes;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestTaskGSController
    {
        public TestTaskGSController()
        {
            this.context = TestMockingContext.GetContext();
            SessionManager sessionManager = new SessionManager(context);
            controller = new TaskGSController(context);
        }
        public TaskGSController controller;
        public Context context;
        
        [Test]
        public void Create()
        {
            User user = TestMockingContext.CreateUser();
            IGAccount account = TestMockingContext.CreateSession(user.userId);
            JObject json = JsonConvert.DeserializeObject<dynamic>(            
            "{" +
                "\"user_token\": \"" + user.userToken + "\"," +
                "\"session_id\" : " +  account.accountId + "," + 
                "\"task_type\" : 1, " + 
                "\"task_subtype\" : 3, " + 
                "\"task_data\" : " + 
                "{ \"usernames\": [ \"therock\" ] }," + 
                "\"task_options\" : " + 
                "{ " + 
                    "\"like_users_post\": false, " + 
                    "\"watch_stories\": false, " + 
                    "\"dont_follow_on_private\" : false, " + 
                    "\"auto_unfollow\" : false " + 
                "} " +
            "}");
            ActionResult<dynamic> success = controller.Create(json);
            Assert.AreEqual(success.Value.success, true);
        }
        [Test]
        public void Delete()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            controller.context = TestMockingContext.GetContext();
            TaskCache cache = new TaskCache() {
                user_token = task.account.User.userToken,
                task_id = task.taskId
            };
            ActionResult<dynamic> success = controller.Delete(cache);
            ActionResult<dynamic> unsuccess = controller.Delete(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void SelectAll()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            controller.context = TestMockingContext.GetContext();
            TaskCache cache = new TaskCache() {
                user_token = task.account.User.userToken,
                session_id = task.account.accountId
            };
            ActionResult<dynamic> success = controller.SelectAll(cache);
            Assert.AreEqual(success.Value.success, true);
        }
        [Test]
        public void Select()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            controller.context = TestMockingContext.GetContext();
            TaskCache taskCache = new TaskCache() {
                user_token = task.account.User.userToken,
                task_id = task.taskId
            };
            ActionResult<dynamic> success = controller.Select(taskCache);
            Assert.AreEqual(success.Value.success, true);
        }
        [Test]
        public void StartStop()
        {
            TaskGS task = TestMockingContext.CreateTaskGSEnviroment();
            controller.context = TestMockingContext.GetContext();
            TaskCache taskCache = new TaskCache() {
                user_token = task.account.User.userToken,
                task_id = task.taskId
            };
            ActionResult<dynamic> success = controller.StartStop(taskCache);
            Assert.AreEqual(success.Value.success, true);
        }
    }
}