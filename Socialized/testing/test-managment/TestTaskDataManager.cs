using Serilog;
using NUnit.Framework;
using System.Collections.Generic;

using Controllers;
using database.context;
using InstagramService;
using Models.Common;
using Models.SessionComponents;
using Models.GettingSubscribes;

namespace Testing.Managment
{
    [TestFixture]
    public class TestTaskDataManager
    {
        public TestTaskDataManager()
        {
            context = TestMockingContext.GetContext();
            this.manager = new TaskDataManager(new LoggerConfiguration().CreateLogger(),context);
        }
        public Context context;
        public TaskDataManager manager;
        public string message;
        [Test]
        public void GetNonDeleteData()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskData success = manager.GetNonDeleteData(data.dataId, "test",ref message);
            TaskData unsuccess = manager.GetNonDeleteData(data.dataId, null, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetNonDeleteDataList()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            List<TaskData> success = manager.GetNonDeleteDataList(data.taskId);
            List<TaskData> unsuccess = manager.GetNonDeleteDataList(0);
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void GetNonDeleteTask()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskGS success = manager.GetNonDeleteTask(data.taskId, ref message);
            TaskGS unsuccess = manager.GetNonDeleteTask(0, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetNonDeleteTaskUser()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskGS success = manager.GetNonDeleteTask(data.taskId, "test", ref message);
            TaskGS unsuccess = manager.GetNonDeleteTask(data.taskId, null, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void StopTask()
        {
            User user = TestMockingContext.CreateUser();
            IGAccount account = TestMockingContext.CreateSession(user.userId);
            TaskGS task = TestMockingContext.CreateTask(account.accountId);
            TaskData data = TestMockingContext.CreateTaskData(task.taskId);
            List<TaskData> taskData = new List<TaskData>();
            taskData.Add(data);
            manager.StopTask(ref task, taskData);
            task = null;
            manager.StopTask(ref task, taskData);
        }
        [Test]
        public void DeleteTask()
        {
            TaskGS task = new TaskGS();
            manager.DeleteTask(ref task, 0);
            task = null;
            manager.DeleteTask(ref task, 1);
        }
        [Test]
        public void DeleteStopTask()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            manager.DeleteStopTask(data.taskId);
            manager.DeleteStopTask(0);
        }
        [Test]
        public void DeleteData()
        {
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            manager.DeleteData(data, ref message);
        }
        [Test]
        public void StartStopData()
        {   
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            manager.StartStopData(data, ref message);
        }
        [Test]
        public void StartStopTask()
        {   
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            manager.StartStopTask(data.taskId, true);
            manager.StartStopTask(data.taskId, false);
            manager.StartStopTask(0, true);
        }
        [Test]
        public void GetTaskData()
        {   
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            TaskCache cache = new TaskCache()
            {
                username = "test"
            };
            TaskData success = manager.GetTaskData(TaskSubtype.ByCommentators, cache, ref message);
            TaskData unsuccess = manager.GetTaskData(TaskSubtype.ByHashtag, cache, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetLocation()
        {
            TaskCache cache = new TaskCache()
            {
                location_name = "test",
                latitude = 1.0,
                longitude = 2.0
            };
            TaskData success = manager.GetLocation(cache, ref message);
            cache.location_name = "";
            TaskData unsuccess = manager.GetLocation(cache, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetHashtag()
        {
            TaskCache cache = new TaskCache()
            {
                hashtag = "test"
            };
            TaskData success = manager.GetHashtag(cache, ref message);
            cache.hashtag = "";
            TaskData unsuccess = manager.GetHashtag(cache, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUsername()
        {
            TaskCache cache = new TaskCache()
            {
                username = "test"
            };
            TaskData success = manager.GetUsername(cache, ref message);
            cache.username = "";
            TaskData unsuccess = manager.GetUsername(cache, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetTaskDataOutput()
        {
            TaskData data = new TaskData();
            dynamic success = manager.GetTaskDataOutput(data);
            dynamic unsuccess = manager.GetTaskDataOutput(null);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
    }
}