using Serilog;
using NUnit.Framework;

using Managment;
using socialized;
using Controllers;
using database.context;
using Models.Common;
using Models.AdminPanel;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestSupportController
    {
        Context context;
        SupportController controller;

        public TestSupportController()
        {
            AuthOptions authOptions = new AuthOptions();
            this.context = TestMockingContext.GetContext();
            this.controller = new SupportController(this.context);
            this.controller.log = new LoggerConfiguration().CreateLogger();
        }
        [Test]
        public void CreateAppeal()
        {
            User user = TestMockingContext.CreateUser();
            SupportCache cache = new SupportCache() {
                user_token = user.userToken,
                appeal_subject = TestMockingContext.values.post_subject
            };
            var result = controller.CreateAppeal(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void UGetAppeals()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            var result = controller.UGetAppeals(0,1);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void AGetAppeals()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            var result = controller.AGetAppeals(0, 1);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void AEndAppeal()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            SupportCache cache = new SupportCache() {
                appeal_id = appeal.appealId
            };
            var result = controller.AEndAppeal(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void UEndAppeal()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            SupportCache cache = new SupportCache() {
                appeal_id = appeal.appealId,
                user_token = appeal.user.userToken
            };
            var result = controller.UEndAppeal(cache);
            Assert.AreEqual(result.Value.success, true);
        }

        [Test]
        public void USendMessages()
        {
            string jsonData;
            Appeal appeal;
            Dictionary<string, StringValues> fields = new Dictionary<string, StringValues>();
            IFormCollection collection;

            appeal = TestMockingContext.CreateAppealEnviroment();

            jsonData= "{ \"user_token\" : \"" + appeal.user.userToken + "\", \"appeal_id\" : " + appeal.appealId 
                + ", \"appeal_message\" : \"Hello world!\" }";
            fields.Add("data", jsonData);
            collection = new FormCollection(fields, null);
            var result = controller.USendMessage(collection);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void ASendMessages()
        {
            Appeal appeal; Admin admin;
            IFormCollection collection;
            Dictionary<string, StringValues> fields = new Dictionary<string, StringValues>();
            
            appeal = TestMockingContext.CreateAppealEnviroment(); 
            admin = TestMockingContext.CreateAdmin();
            fields.Add("data", "{\"appeal_id\" : " + appeal.appealId + ",\"appeal_message\" : null}");
            collection = new FormCollection(fields, null);
            
            var result = controller.ASendMessage(collection);
            // Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void UMessages()
        {
            AppealMessage message = TestMockingContext.CreateAppealMessageEnviroment();
            var messages = controller.UMessages(message.appealId, 0, 1);
            Assert.AreEqual(messages.Value.success, true);
        }
        [Test]
        public void AMessages()
        {
            AppealMessage message = TestMockingContext.CreateAppealMessageEnviromentWithAdmin();
            var messages = controller.AMessages(message.appealId, 0, 1);
            Assert.AreEqual(messages.Value.success, true);
        }
    }
}