using NUnit.Framework;
using System.Collections.Generic;

using Managment;
using InstagramService;
using database.context;
using Models.Common;
using Models.SessionComponents;
using Models.GettingSubscribes;

namespace UseCases.Services.Tests
{
    [TestFixture]
    public class TaskGSManagerTests
    {
        public string userEmail = "test6574839210@gmail.com";
        public string userToken = "1234";
        public string message = null;
        public Context context;
        public TaskGSManager tasksManager;
        public TaskGSManagerTests()
        {
            context = MockingContextTests.GetContext();
            this.tasksManager = new TaskGSManager(context);
        }
        public TaskGS CreateGSEnviroment()
        {
            User user = MockingContextTests.CreateUser();
            IGAccount account = MockingContextTests.CreateSession(user.userId);
            TaskGS task = MockingContextTests.CreateTask(account.accountId);
            TaskData taskData = MockingContextTests.CreateTaskData(task.taskId);
            account.User = user;
            task.account = account;
            taskData.Task = task;
            return task;
        }
        [Test]
        public void SelectTask()
        {
            TaskGS task = CreateGSEnviroment();
            TaskGS success = tasksManager.SelectTask(task.taskId, task.account.userId, ref message);
            TaskGS unsuccess = tasksManager.SelectTask(-1, task.account.userId, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetNonDelete()
        {
            TaskGS task = CreateGSEnviroment();
            TaskGS success = tasksManager.GetNonDelete(task.taskId, task.account.userId);
            TaskGS unsuccess = tasksManager.GetNonDelete(-1, task.account.userId);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void Delete()
        {
            TaskGS task = CreateGSEnviroment();
            bool success = tasksManager.Delete(task.taskId, task.account.userId, ref message);
            bool unsuccess = tasksManager.Delete(-1, task.account.userId, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void StartStopTask()
        {
            TaskGS task = CreateGSEnviroment();
            bool success = tasksManager.StartStopTask(task.taskId, task.account.userId, ref message);
            bool unsuccess = tasksManager.StartStopTask(-1, task.account.userId, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetNonDeleteTaskData()
        {
            TaskGS task = CreateGSEnviroment();
            var success = tasksManager.GetNonDeleteTaskData(task.taskId);
            List<TaskData> unsuccess = tasksManager.GetNonDeleteTaskData(-1);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void StartTask()
        {
            TaskGS task = CreateGSEnviroment();
            bool success = tasksManager.StartTask(task, ref message);
            bool unsuccess = tasksManager.StartTask(null, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void StopTask()
        {
            TaskGS task = CreateGSEnviroment();
            bool success = tasksManager.StopTask(task, ref message);
            bool unsuccess = tasksManager.StopTask(null, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void UpdateTaskDataStopped()
        {
            TaskGS task = CreateGSEnviroment();
            tasksManager.UpdateTaskDataStopped(task.taskData, true);
            tasksManager.UpdateTaskDataStopped(task.taskData, false);
        }
        [Test]
        public void SelectCurrentTask()
        {
            TaskGS task = CreateGSEnviroment();
            dynamic success = tasksManager.SelectCurrentTask(task);
            dynamic unsuccess = tasksManager.SelectCurrentTask(null);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void UpdateTask()
        {
            TaskGS task = CreateGSEnviroment();
            tasksManager.UpdateTask(task);
        }
        [Test]
        public void SelectTasks()
        {
            TaskGS task = CreateGSEnviroment();
            List<dynamic> success = tasksManager.SelectTasks(task.account.userId, task.account.accountId, ref message);
            List<dynamic> unsuccess = tasksManager.SelectTasks(task.account.userId, -1, ref message);
            Assert.AreNotEqual(success, null);
        }
        [Test]
        public void SortTaskByType()
        {
            TaskGS task = CreateGSEnviroment();
            List<TaskGS> tasks = new List<TaskGS>();
            tasks.Add(task);
            List<sbyte> types = new List<sbyte>();
            types.Add(task.taskType);
            List<dynamic> success = tasksManager.SortTaskByType(tasks, types);
            List<dynamic> unsuccess = tasksManager.SortTaskByType(tasks, null);
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void AddSortedTaskByType()
        {
            TaskGS task = CreateGSEnviroment();
            List<TaskGS> tasks = new List<TaskGS>();
            tasks.Add(task);
            List<dynamic> success = tasksManager.AddSortedTaskByType(tasks, task.taskType);
            List<dynamic> unsuccess = tasksManager.AddSortedTaskByType(null, task.taskType);
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void GetTaskTypes()
        {
            TaskGS task = CreateGSEnviroment();
            List<TaskGS> tasks = new List<TaskGS>();
            tasks.Add(task);
            List<sbyte> success = tasksManager.GetTaskTypes(tasks);
            List<sbyte> unsuccess = tasksManager.GetTaskTypes(null);
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void GetTaskWithTaskData()
        {
            TaskGS task = CreateGSEnviroment();
            List<TaskGS> success = tasksManager.GetTaskWithTaskData(task.account.accountId);
            List<TaskGS> unsuccess = tasksManager.GetTaskWithTaskData(-1);
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void GetNonDeleteSession()
        {
            TaskGS task = CreateGSEnviroment();
            IGAccount success = tasksManager.GetNonDeleteSession(task.account.userId, task.account.accountId);
            IGAccount unsuccess = tasksManager.GetNonDeleteSession(task.account.userId, -1);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetTaskOptionsOutput()
        {
            TaskOption option = new TaskOption();
            dynamic success = tasksManager.GetTaskOptionsOutput(TaskType.Following, option);
            dynamic unsuccess = tasksManager.GetTaskOptionsOutput(TaskType.Following, null);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null); 
        }
        [Test]
        public void GetTaskTypeName()
        {
            string success = tasksManager.GetTaskTypeName(1);
            string unsuccess = tasksManager.GetTaskTypeName(10);
            Assert.AreNotEqual(success, "");
            Assert.AreEqual(unsuccess, "");
        }
        [Test]
        public void GetTaskDataOutput()
        {
            ICollection<TaskData> tasks = new List<TaskData>();
            TaskData task = new TaskData();
            tasks.Add(task);
            List<dynamic> success = tasksManager.GetTaskDataOutput(tasks);
            List<dynamic> unsuccess = tasksManager.GetTaskDataOutput(null);
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void GetTaskSubtypeName()
        {
            string success = tasksManager.GetTaskSubtypeName(1);
            string unsuccess = tasksManager.GetTaskSubtypeName(10);
            Assert.AreNotEqual(success, "");
            Assert.AreEqual(unsuccess, "");
        }
    }
}