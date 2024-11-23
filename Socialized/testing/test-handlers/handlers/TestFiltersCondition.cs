using Serilog;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using Controllers;
using Models.GettingSubscribes;

namespace Testing.Handlers
{
    [TestFixture]
    public class TestFiltersCondition
    {
        public JObject json = null;
        public TestFiltersCondition()
        {
            json = JsonConvert.DeserializeObject<dynamic>(
            "{" +
                "\"task_filters\" :" +
                "{" +
                    "\"range_subscribers_from\" : 1, " +
                    "\"range_subscribers_to\" : 100, " +
                    "\"range_following_from\" : 1, " +
                    "\"range_following_to\" : 100, " +
                    "\"publication_count\" : 10, " +
                    "\"latest_publication_no_younger\" : 1, " +
                    "\"without_profile_photo\" : true, " +
                    "\"with_profile_url\" : true, " +
                    "\"english\" : true," +
                    "\"ukrainian\" : true," +
                    "\"russian\" : true," +
                    "\"arabian\" : false," +
                    "\"words_in_description\" : [\"Hello\",\"my\",\"world!\"]," +
                    "\"no_words_in_description\" : [\"Hello\",\"your\",\"world!\"]" +
                "}" +
            "}"
            );
        }
        public FiltersCondition handler = new FiltersCondition(new LoggerConfiguration().CreateLogger());
        public string message = "";
        [Test]
        public void handle()
        {
            TaskGS task = new TaskGS();
            task.taskId = 1;
            task.taskType = 1;
            bool success = handler.handle(ref json, ref task, ref message);
            json = JsonConvert.DeserializeObject<dynamic>("{ \"task_options\" : true }");
            bool successWithout = handler.handle(ref json, ref task, ref message);
            Assert.AreEqual(true, success);
            Assert.AreEqual(true, successWithout);
        }
        [Test]
        public void CreateFilter()
        {
            TaskFilter success = handler.CreateFilter(json);
            Assert.AreEqual(success.range_subscribers_from, 1);
        }
        [Test]
        public void GetFilterWords()
        {
            TaskFilter filter = new TaskFilter();
            filter.words_in_description = new List<string>();
            filter.words_in_description.Add("test");
            handler.GetFilterWords(ref filter);
        }
    }
}