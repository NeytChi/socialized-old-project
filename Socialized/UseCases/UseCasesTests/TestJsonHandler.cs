using Serilog;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UseCasesTasks;

namespace UseCasesTests
{
    [TestFixture]
    public class TestJsonHandler
    {
        [Test]
        public void handle()
        {
            string message = "", userToken = "N3RzTACj5UL4Ez5t1yP7PI0RZ9XDpwmvKhNdSs4o";
            JsonHandler jsonHandler = new JsonHandler(new LoggerConfiguration().CreateLogger());
            JObject json = JsonConvert.DeserializeObject<dynamic>("{ \"user_token\": \"" + userToken + "\" }");
            Assert.AreEqual(jsonHandler.handle(ref json, "user_token", JTokenType.String, ref message).ToString(), userToken);
            Assert.AreEqual(jsonHandler.handle(ref json, "user_token", JTokenType.Integer, ref message), null);
            Assert.AreEqual(jsonHandler.handle(ref json, "token", JTokenType.String, ref message), null);
        }
    }
}