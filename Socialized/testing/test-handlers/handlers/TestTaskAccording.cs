using Serilog;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

using Controllers;
using InstagramService;
using Models.GettingSubscribes;

namespace Testing.Handlers
{
    [TestFixture]
    public class TestTaskAccording
    {
        public TaskAccording handler = new TaskAccording(new LoggerConfiguration().CreateLogger());
        public string message = "";
        [Test]
        public void handle()
        {
            JObject json = new JObject();
            TaskGS task = new TaskGS();
            task.taskType = 1;
            task.taskSubtype = 1;
            bool success = handler.handle(ref json, ref task, ref message);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void AccordingTask()
        {
            bool success = handler.AccordingTask(TaskType.Following, TaskSubtype.ByCommentators, ref message);
            bool unsuccess = handler.AccordingTask(TaskType.Following, TaskSubtype.Comment, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void BaseTasks()
        {
            bool success = handler.BaseTasks(TaskSubtype.ByCommentators, ref message);
            bool unsuccess = handler.BaseTasks(TaskSubtype.Comment, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CutOffTasks()
        {
            bool success = handler.CutOffTasks(TaskSubtype.ByList, ref message);
            bool unsuccess = handler.CutOffTasks(TaskSubtype.ByHashtag, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CommentTasks()
        {
            bool success = handler.CommentTasks(TaskSubtype.Comment, ref message);
            bool unsuccess = handler.CommentTasks(TaskSubtype.ByHashtag, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}