using Serilog;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using Controllers;
using InstagramService;
using Models.GettingSubscribes;

namespace Testing.Handlers
{
    [TestFixture]
    public class TestTaskDataHandler
    {
        public string taskData = "{{ \"task_data\" : {0} }}";
        public string locations = 
        "{" +
            "\"locations\":" +
            "[" +
                "\"London\", \"Paris\", \"Kiev\", \"Tokio\"" +
            "]," +
            "\"longitudes\":" +
            "[" +
                "12.02219312, 21.123123, 921.0202, 9.219013123" +
            "]," +
            "\"latitudes\":" +
            "[" +
                "12.02219312, 21.123123, 921.0202, 9.219013123" +
            "]" +
        "}";
        public string usernames =
        "{" +
            "\"usernames\":" +
            "[" +
                "\"annanystrom\", \"adult_tale\", \"laurendrainfit\", \"brittanyperilleee\"" +
            "]" +
        "}";
        public string hashtags = 
        "{" +
            "\"hashtags\":" +
            "[" +
                "\"spring\", \"winter\"" +
            "]" +
        "}";
        public string comment =
        "{" +
            "\"comment\": \"Hello World!\"," +
            "\"usernames\":" +
            "[" +
                "\"annanystrom\", \"adult_tale\", \"laurendrainfit\", \"brittanyperilleee\"" +
            "]" +
        "}";
        public TaskCondition handler = new TaskCondition(new LoggerConfiguration().CreateLogger());
        public string message = "";
        [Test]
        public void handle()
        {
            TaskGS task = new TaskGS();
            task.taskSubtype = 1;
            JObject json = JsonConvert.DeserializeObject<dynamic>
            (
                string.Format(taskData, usernames)
            );
            bool success = handler.handle(ref json, ref task, ref message);
            task.taskSubtype = 7;
            bool unsuccess = handler.handle(ref json, ref task, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetData()
        {
            JObject json = JsonConvert.DeserializeObject<dynamic>
            (
                string.Format(taskData, usernames)
            );
            HashSet<TaskData> success = handler.GetData((TaskSubtype)1, ref json, ref message);
            HashSet<TaskData> unsuccess = handler.GetData((TaskSubtype)7, ref json, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetCommentData()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                JObject json = JsonConvert.DeserializeObject<dynamic>(comment);
                TaskData success = handler.GetCommentData(json, ref message);
                TaskData unsuccess = handler.GetCommentData(null, ref message);
                Assert.AreNotEqual(success, null);
                Assert.AreEqual(unsuccess, null);
            }
        }
        [Test]
        public void GetNamedData()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                JObject json = JsonConvert.DeserializeObject<dynamic>(usernames);
                HashSet<TaskData> success = handler.GetNamedData(json, "usernames", ref message);
                HashSet<TaskData> unsuccess = handler.GetNamedData(null, "usernames", ref message);
                Assert.AreNotEqual(success, null);
                Assert.AreEqual(unsuccess, null);
            }
        }
        [Test]
        public void GetLocationData()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                JObject json = JsonConvert.DeserializeObject<dynamic>(locations);
                HashSet<TaskData> success = handler.GetLocationData(json, ref message);
                HashSet<TaskData> unsuccess = handler.GetLocationData(null, ref message);
                Assert.AreNotEqual(success, null);
                Assert.AreEqual(unsuccess, null);
            }
        }
    }
}