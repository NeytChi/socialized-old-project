using Serilog;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using InstagramService.Statistics;

namespace Testing.Service
{
    [TestFixture]
    public class TestStatisticsService
    {
        public StatisticsService service = new StatisticsService(new LoggerConfiguration().CreateLogger());
        
        [Test]
        public void GetFacebookAccount()
        {
            // if (TestMockingContext.values.send_request_to_instagram_service) {
            //     string success = service.GetFacebookAccount(
            //         TestMockingContext.values.access_token);
            //     Assert.AreEqual(success, TestMockingContext.values.facebook_id);
            // }
        }
        [Test]
        public void PullOutFacebookAccount()
        {
            // if (TestMockingContext.values.send_request_to_instagram_service) {
            //     JObject json = JsonConvert.DeserializeObject<JObject>("{\"data\" : [{\"id\" : \"" 
            //         + TestMockingContext.values.facebook_id + "\"\n}\n]\n}");
            //     string success = service.PullOutFacebookAccount(json);
            //     Assert.AreEqual(success, TestMockingContext.values.facebook_id);
            // }
        }
        [Test]
        public void GetBussinessAccountId()
        {
            if (TestMockingContext.values.send_request_to_instagram_service) {
                string success = service.GetBussinessAccountId(
                    TestMockingContext.values.facebook_id,
                    TestMockingContext.values.access_token);
                Assert.AreEqual(success, TestMockingContext.values.business_account_id);
            }
        }
        [Test]
        public void PullOutBussinessAccountId()
        {
            JObject json = JsonConvert.DeserializeObject<JObject>("{\"instagram_business_account\" :" + "{\"id\": \"" 
                + TestMockingContext.values.business_account_id + "\"},\"id\": \"" 
                + TestMockingContext.values.facebook_id + "\"}");
            string success = service.PullOutBussinessAccountId(json);
            Assert.AreEqual(success, TestMockingContext.values.business_account_id);
        }
        [Test]
        public void GetLongTermAccessToken()
        {
            string longTermAccessToken = service.GetLongTermAccessToken(TestMockingContext.values.access_token);
            Assert.AreNotEqual(longTermAccessToken, null);
        }
        [Test]
        public void GetUsername()
        {
            string username = service.GetUsername(TestMockingContext.values.business_account_id, 
                TestMockingContext.values.access_token);
            Assert.AreNotEqual(username, null);
        }
    }
}