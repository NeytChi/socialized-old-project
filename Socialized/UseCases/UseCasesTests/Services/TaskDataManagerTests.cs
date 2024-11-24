using Serilog;
using NUnit.Framework;
using System.Collections.Generic;

using UseCasesTasks;
using database.context;
using InstagramService;
using Models.Common;
using Models.SessionComponents;
using Models.GettingSubscribes;

namespace UseCases.Services.Tests
{
    [TestFixture]
    public class TaskDataManagerTests
    {
        public TaskDataManagerTests()
        {
            context = MockingContextTests.GetContext();
            this.manager = new TaskDataManager(new LoggerConfiguration().CreateLogger(),context);
        }
        public Context context;
        public TaskDataManager manager;
        public string message;
        [Test]
        public void GetNonDeleteData()
        {
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
            TaskData success = manager.GetNonDeleteData(data.dataId, "test",ref message);
            TaskData unsuccess = manager.GetNonDeleteData(data.dataId, null, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetNonDeleteDataList()
        {
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
            List<TaskData> success = manager.GetNonDeleteDataList(data.taskId);
            List<TaskData> unsuccess = manager.GetNonDeleteDataList(0);
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void GetNonDeleteTask()
        {
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
            TaskGS success = manager.GetNonDeleteTask(data.taskId, ref message);
            TaskGS unsuccess = manager.GetNonDeleteTask(0, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetNonDeleteTaskUser()
        {
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
            TaskGS success = manager.GetNonDeleteTask(data.taskId, "test", ref message);
            TaskGS unsuccess = manager.GetNonDeleteTask(data.taskId, null, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void StopTask()
        {
            User user = MockingContextTests.CreateUser();
            IGAccount account = MockingContextTests.CreateSession(user.userId);
            TaskGS task = MockingContextTests.CreateTask(account.accountId);
            TaskData data = MockingContextTests.CreateTaskData(task.taskId);
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
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
            manager.DeleteStopTask(data.taskId);
            manager.DeleteStopTask(0);
        }
        [Test]
        public void DeleteData()
        {
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
            manager.DeleteData(data, ref message);
        }
        [Test]
        public void StartStopData()
        {   
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
            manager.StartStopData(data, ref message);
        }
        [Test]
        public void StartStopTask()
        {   
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
            manager.StartStopTask(data.taskId, true);
            manager.StartStopTask(data.taskId, false);
            manager.StartStopTask(0, true);
        }
        [Test]
        public void GetTaskData()
        {   
            TaskData data = MockingContextTests.CreateTaskDataEnviroment();
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