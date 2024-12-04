using Serilog;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UseCasesTasks;
using Models.GettingSubscribes;

namespace UseCasesTests.Tasks.Tests
{
    [TestFixture]
    public class TestTaskDefineHandler
    {
        public TaskDefine handler = new TaskDefine(new LoggerConfiguration().CreateLogger());
        public string message = "";
        [Test]
        public void handle()
        {
            TaskGS task = new TaskGS();
            JObject json = JsonConvert.DeserializeObject<dynamic>("{ \"task_type\" : 1, \"task_subtype\" : 3 }");
            bool success = handler.handle(ref json, ref task, ref message);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void DefineSbyte()
        {
            JObject json = JsonConvert.DeserializeObject<dynamic>("{ \"task_type\" : 1, \"task_subtype\" : 3 }");
            sbyte success = handler.DefineSbyte(ref json, "task_type", ref message);
            Assert.AreEqual(success, 1);
        }
        [Test]
        public void DefineSbyteWithNonExistsTaskType()
        {
            JObject json = JsonConvert.DeserializeObject<dynamic>("{ \"task_type\" : \"128\", \"task_subtype\" : 3 }");
            sbyte unsuccess = handler.DefineSbyte(ref json, "task_type", ref message);
            Assert.AreEqual(unsuccess, -1);
        }
    }
}