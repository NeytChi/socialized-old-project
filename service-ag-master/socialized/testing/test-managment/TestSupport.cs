using Serilog;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

using Managment;
using database.context;
using Models.Common;
using Models.AdminPanel;
using Microsoft.Extensions.Primitives;

namespace Testing.Managment
{
    [TestFixture]
    public class TestSupport
    {
        Support support;
        string error;
        Context context;
            
        public TestSupport()
        {
            this.context = TestMockingContext.GetContext();
            this.support = new Support(new LoggerConfiguration().CreateLogger(), context);
        }
        [Test]
        public void CreateAppeal()
        {
            User user = TestMockingContext.CreateUser();
            SupportCache cache = new SupportCache() {
                user_token = user.userToken,
                appeal_subject = TestMockingContext.values.post_subject
            };
            var result = support.CreateAppeal(cache, ref error);
            Assert.AreEqual(result.userId, user.userId);
        }
        [Test]
        public void SubjectIsTrue()
        {
            Assert.AreEqual(support.SubjectIsTrue(
                TestMockingContext.values.post_subject, ref error), true);
            Assert.AreEqual(support.SubjectIsTrue("", ref error), false);
            Assert.AreEqual(support.SubjectIsTrue(null, ref error), false);
        }
        [Test]
        public void GetAppealsByUser()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            SupportCache cache = new SupportCache() {
                user_token = appeal.user.userToken,
                since = 0,
                count = 1
            };
            var result = support.GetAppealsByUser(cache.user_token, cache.since, cache.count);
            Assert.AreEqual(result.Length, 1);
        }
        [Test]
        public void GetAppealsByAdmin()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            var result = support.GetAppealsByAdmin(0, 1);
            Assert.AreEqual(result.Length, 1);
        }
        [Test]
        public void EndAppeal()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            Assert.AreEqual(support.EndAppeal(appeal.appealId, ref error), true);
        }
        [Test]
        public void GetAppeal()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            Assert.AreEqual(support.GetAppeal(appeal.appealId, ref error).appealId, appeal.appealId);
        }
        [Test]
        public void GetAppeal_WithUserToken()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment(); 
            Assert.AreEqual(support.GetAppeal(appeal.appealId, appeal.user.userToken, 
                ref error).appealId, appeal.appealId);
        }
        [Test]
        public void GetNonDeleteUser()
        {
            User user = TestMockingContext.CreateUser(); 
            Assert.AreEqual(support.GetNonDeleteUser(user.userToken, ref error).userId, 
                user.userId);
        }
        [Test]
        public void AppealFilesIsTrue()
        {
            List<IFormFile> files = new List<IFormFile>();
            IFormFile file = TestMockingContext.CreateFile();
            files.Add(file);
            Assert.AreEqual(support.AppealFilesIsTrue(ref files), true);
        }
        [Test]
        public void AppealFilesIsTrue_More_That_Three()
        {
            List<IFormFile> files = new List<IFormFile>();
            IFormFile file = TestMockingContext.CreateFile();
            files.Add(file);
            files.Add(file);
            files.Add(file);
            files.Add(file);
            Assert.AreEqual(support.AppealFilesIsTrue(ref files), true);
            Assert.AreEqual(files.Count, 3);
        }
        [Test]
        public void AppealFilesIsTrue_Null()
        {
            List<IFormFile> files = null;
            Assert.AreEqual(support.AppealFilesIsTrue(ref files), false);
        }
        [Test]
        public void MessageTextIsTrue()
        {
            Assert.AreEqual(support.MessageTextIsTrue(TestMockingContext.values.post_htmltext, 
                ref error), true);
            Assert.AreEqual(support.MessageTextIsTrue("", ref error), false);
            Assert.AreEqual(support.MessageTextIsTrue(null, ref error), false);
        }
        [Test]
        public void AppealMessageIsTrue()
        {
            SupportCache cache = new SupportCache() {
                appeal_message = TestMockingContext.values.post_htmltext
            };
            Assert.AreEqual(support.AppealMessageIsTrue(cache, ref error), true);
            var files = new List<IFormFile>();
            files.Add(TestMockingContext.CreateFile());
            cache.appeal_message = null;
            cache.files = files;
            Assert.AreEqual(support.AppealMessageIsTrue(cache, ref error), true);
            cache.files = null;
            Assert.AreEqual(support.AppealMessageIsTrue(cache, ref error), false);
        }
        [Test]
        public void AddFilesToMessage()
        {
            var files = new List<IFormFile>();
            files.Add(TestMockingContext.CreateFile());
            var result = support.AddFilesToMessage(files, 0);
            Assert.AreEqual(result.Count, 1);
        }
        [Test]
        public void SendMessage_Like_Admin()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment();
            var files = new List<IFormFile>();
            files.Add(TestMockingContext.CreateFile());
            Admin admin = TestMockingContext.CreateAdmin();
            SupportCache cache = new SupportCache() {
                appeal_id = appeal.appealId,
                appeal_message = TestMockingContext.values.post_htmltext,
                files = files,
                admin_id = admin.adminId
            };
            Assert.AreEqual(support.SendMessage(cache, ref error).appealId, appeal.appealId);
        }
        [Test]
        public void SendMessage_Like_User()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment();
            var files = new List<IFormFile>();
            files.Add(TestMockingContext.CreateFile());
            Admin admin = TestMockingContext.CreateAdmin();
            SupportCache cache = new SupportCache() {
                user_token = appeal.user.userToken,
                appeal_id = appeal.appealId,
                appeal_message = TestMockingContext.values.post_htmltext,
                files = files,
            };
            Assert.AreEqual(support.SendMessage(cache, ref error).appealId, appeal.appealId);
        }
        [Test]
        public void GetAppealMessages()
        {
            AppealMessage message = TestMockingContext.CreateAppealMessageEnviroment();
            var messages = support.GetAppealMessages(message.appealId, 0, 1);
            Assert.AreEqual(messages.Length, 1);
        }
        [Test]
        public void GetAppealMessagesWithAdmin()
        {
            AppealMessage message = TestMockingContext.CreateAppealMessageEnviromentWithAdmin();
            var messages = support.GetAppealMessages(message.appealId, 0, 1);
            Assert.AreEqual(messages.Length, 1);
        }
        [Test]
        public void GetSender_Admin()
        {
            Admin admin = TestMockingContext.CreateAdmin();
            List<Admin> admins = new List<Admin>();
            admins.Add(admin);
            dynamic result = support.GetSender(admins);
            Assert.AreEqual(result.admin_fullname, admin.adminFullname);
        }
        [Test]
        public void GetSender_User()
        {
            User user = TestMockingContext.CreateUser();
            dynamic result = support.GetSender(user);
        }
        [Test]
        public void UpdateAnsweredAppeal()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment();
            appeal.appealState = 2;
            context.Appeals.Update(appeal);
            context.SaveChanges();
            Assert.AreEqual(support.UpdateAnsweredAppeal(appeal), true);
        }
        [Test]
        public void UpdateAnsweredAppeal_On_A_Answered_Appeal()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment();
            appeal.appealState = 4;
            context.Appeals.Update(appeal);
            context.SaveChanges();
            Assert.AreEqual(support.UpdateAnsweredAppeal(appeal), false);
        }
        [Test]
        public void UpdateReadAppeal()
        {
            Appeal appeal = TestMockingContext.CreateAppealEnviroment();
            appeal.appealState = 1;
            context.Appeals.Update(appeal);
            context.SaveChanges();
            support.UpdateReadAppeal(appeal.appealId, ref error);
        }
        [Test]
        public void GetCacheFromData()
        {
            string jsonData;
            SupportCache cache = new SupportCache();
            Dictionary<string, StringValues> fields = new Dictionary<string, StringValues>();
            IFormCollection collection;

            jsonData= "{ \"user_token\" : \"{{user_token}}\", \"appeal_id\" : 1, \"appeal_message\" : \"Hello world!\" }";
            fields.Add("data", jsonData);
            collection = new FormCollection(fields, null);
            Assert.AreEqual(support.GetCacheFromData(collection, ref cache, ref error), true);
            Assert.AreEqual(support.GetCacheFromData(collection, ref cache, ref error), true);
        }
    }
}