using Serilog;
using NUnit.Framework;
using System.Collections.Generic;

using Managment;
using Controllers;
using database.context;
using InstagramService;
using InstagramApiSharp.API.Builder;
using Models.Common;
using Models.GettingSubscribes;
using Models.SessionComponents;

namespace Testing.Handlers
{
    [TestFixture]
    public class TestTaskDataCheckHandler
    {
        public SessionManager manager;
        public Context context;
        public TaskDataCondition handler;
        public string message = "";
        public TestTaskDataCheckHandler()
        {
            context = TestMockingContext.GetContext();
            this.manager = new SessionManager(context);
            manager.context = context;
            handler = new TaskDataCondition( new LoggerConfiguration().CreateLogger(), 
                manager, new SessionStateHandler(context));
        }
        [Test]
        public void CheckUsernames()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User user = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(user.userId);
                var taskData = new List<TaskData>();
                TaskData data = new TaskData();
                data.dataNames = "nikachikapika";
                taskData.Add(data);
                bool success = handler.CheckUsernames(taskData, account.accountId, ref message);
                taskData.Remove(data);
                bool resultNull = handler.CheckUsernames(taskData, account.accountId, ref message);
                data.dataNames = "as12d32k4a5sdoo6qw"; 
                taskData.Add(data);
                bool unsuccess = handler.CheckUsernames(taskData, account.accountId, ref message);
                Assert.AreEqual(success, true);
                Assert.AreEqual(resultNull, false);
                Assert.AreEqual(unsuccess, false);
            }
        }
        [Test]
        public void CheckLocations()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User user = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(user.userId);
                var taskData = new HashSet<TaskData>();
                TaskData data = new TaskData();
                data.dataNames = "London";
                data.dataLatitute = 0;
                data.dataLongitute = 0;
                taskData.Add(data);
                bool success = handler.CheckLocations(taskData, account.accountId, ref message);
                taskData.Remove(data);
                data.dataNames = "901249781294104898723"; 
                data.dataLatitute = -1;
                data.dataLongitute = -1;
                bool resultNull = handler.CheckLocations(taskData, account.accountId, ref message);
                taskData.Add(data);
                bool unsuccess = handler.CheckLocations(taskData, account.accountId, ref message);
                Assert.AreEqual(success, true);
                Assert.AreEqual(resultNull, false);
                Assert.AreEqual(unsuccess, false);
            }
        }
        [Test]
        public void CheckHashtags()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User user = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(user.userId);
                var taskData = new HashSet<TaskData>();
                TaskData data = new TaskData();
                data.dataNames = "winter";
                taskData.Add(data);
                bool success = handler.CheckHashtags(taskData, account.accountId, ref message);
                taskData.Remove(data);
                data.dataNames = "as12d32k4a5sdoo6qw"; 
                bool resultNull = handler.CheckHashtags(taskData, account.accountId, ref message);
                taskData.Add(data);
                bool unsuccess = handler.CheckHashtags(taskData, account.accountId, ref message);
                Assert.AreEqual(success, true);
                Assert.AreEqual(resultNull, false);
                Assert.AreEqual(unsuccess, false);
            }
        }
        [Test]
        public void CheckExistUser()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User user = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(user.userId);
                Session session = manager.LoadSession(account.accountId);
                bool success = handler.CheckExistUser(ref session, "nikachikapika", ref message);
                bool unsuccess = handler.CheckExistUser(ref session, "as12d32k4a5sdoo6qw", ref message);
                Assert.AreEqual(success, true);
                Assert.AreEqual(unsuccess, false);
            }
        }
        [Test]
        public void CheckExistLocation()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User user = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(user.userId);
                Session session = manager.LoadSession(account.accountId);
                bool success = handler.CheckExistLocation(ref session, "London", 0, 0, ref message);
                bool unsuccess = handler.CheckExistLocation(ref session, "901249781294104898723", -1, -1, ref message);
                Assert.AreEqual(success, true);
                Assert.AreEqual(unsuccess, false);
            }
        }
        [Test]
        public void CheckExistHashtag()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                User user = TestMockingContext.CreateUser();
                IGAccount account = TestMockingContext.CreateSession(user.userId);
                Session session = manager.LoadSession(account.accountId);
                bool success = handler.CheckExistHashtag(ref session, "winter", ref message);
                bool unsuccess = handler.CheckExistHashtag(ref session, "as12d32k4a5sdoo6qw", ref message);
                Assert.AreEqual(success, true);
                Assert.AreEqual(unsuccess, false);
            }
        }
    }
}