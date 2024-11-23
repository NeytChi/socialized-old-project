using System;
using Serilog;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Controllers;
using Models.GettingSubscribes;

namespace Testing.Handlers
{
    [TestFixture]
    public class TestOptionsCondition
    {
        public string blocking = "{ \"next_unlocking\" : true }";
        public string commenting = "{ \"watch_stories\": true }";
        public string following =
        "{" +
            "\"like_users_post\": true," +
            "\"watch_stories\": true," +
            "\"dont_follow_on_private\": true," +
            "\"auto_unfollow\": true" +
        "}" ;
        public string unfollowing = 
        "{" +
            "\"like_users_post\": true," +
            "\"without_black_list\": true," +
            "\"unfollow_only_from_non_reciprocal\": true" +
        "}";
        public string liking =
        "{" +
            "\"watch_stories\": true," +
            "\"likes_on_user\": 10" +
        "}";
        public string option = "{{ \"task_options\" : {0} }}";
            

        public OptionsCondition handler = new OptionsCondition(new LoggerConfiguration().CreateLogger());
        public string message = "";
        [Test]
        public void handle()
        {
            TaskGS task = new TaskGS();
            task.taskId = 1;
            task.taskType = 5;
            JObject json = JsonConvert.DeserializeObject<dynamic>(
            string.Format(option, blocking));
            bool success = handler.handle(ref json, ref task, ref message);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void CreateOptions()
        {
            JObject json = JsonConvert.DeserializeObject<dynamic>(
            String.Format(option, blocking));
            TaskOption success = handler.CreateOptions(ref json, ref message);
            Assert.AreEqual(success.nextUnlocking, true);
        }
        [Test]
        public void CheckLikeOption()
        {
            bool success = handler.CheckLikeOption(12, ref message);
            bool unsuccess = handler.CheckLikeOption(0, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}
