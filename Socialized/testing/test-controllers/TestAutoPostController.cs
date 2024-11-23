using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

using Managment;
using Controllers;
using Models.Common;
using database.context;
using InstagramService;
using Models.AutoPosting;
using Models.SessionComponents;
using Testing.Managment;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestAutoPostController
    {
        private AutoPostController controller;
        public Context context;
        public User user;
        public IGAccount account;
        public List<IFormFile> files = new List<IFormFile>(); 
        public AutoPostCache cache;

        public TestAutoPostController()
        {
            this.context = TestMockingContext.GetContext();
            SessionManager manager = new SessionManager(context);
            SessionStateHandler sessionState = new SessionStateHandler(context); 
            this.controller = new AutoPostController(context);
            this.user = TestMockingContext.CreateUser();
            this.account = TestMockingContext.CreateSession(user.userId);
            IFormFile file = TestMockingContext.CreateFile();
            this.cache = TestAutoPostingManager.CreateObjectPost();  
            this.cache.user_token = user.userToken;
            this.cache.session_id = account.accountId;
            files.Add(file);
            files.Add(file); 
        }
        public void DeleteAutoPost()
        {
            AutoPost post = context.AutoPosts.Where(a 
            => a.postLocation == "Kiev").FirstOrDefault();
            if (post != null)
            {
                context.AutoPosts.Remove(post);
                context.SaveChanges();
            }
        }
        public AutoPost CreateAutoPost()
        {
            DeleteAutoPost();
            AutoPost autoPost = new AutoPost()
            {
                sessionId = account.accountId,
                postType = true,
                createdAt = DateTime.Now,
                executeAt = DateTime.Now.AddMonths(2),
                autoDelete = true,
                deleteAfter = DateTime.Now.AddMonths(2),
                postLocation = "Kiev",
                postDescription = "Hello world!",
                postComment = "Hello guys!"
            };
            PostFile file = new PostFile()
            {
                fileOrder = 1,
                filePath = "/test/1234"
            };
            autoPost.files.Add(file);
            context.AutoPosts.Add(autoPost);
            context.SaveChanges();
            return autoPost;
        }
        [Test]
        public void Create()
        {
            string post = JsonConvert.SerializeObject(cache);
            Dictionary<string, StringValues> values = new Dictionary<string, StringValues>();
            values.Add("post", post);
            IFormCollection collection = new FormCollection(values);
            var success = controller.Create(files, collection);
            var unsuccess = controller.Create(null, collection);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void StartStop()
        {
            AutoPost post = CreateAutoPost();
            cache.post_id = post.postId;
            var success = controller.StartStop(cache);
            cache.post_id = 0;
            var unsuccess = controller.StartStop(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void GetByCategory()
        {
            cache.category = 1;
            var success = controller.GetByCategory(cache);
            cache.category = 12;
            var unsuccess = controller.GetByCategory(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void Update()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            var success = controller.Update(cache);
            cache.post_id = 0;
            var unsuccess = controller.Update(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void Delete()
        {
            AutoPost post = CreateAutoPost();
            cache.post_id = post.postId;
            var success = controller.Delete(cache);
            cache.post_id = 0;
            var unsuccess = controller.Delete(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void AddFiles()
        {
            AutoPost post = CreateAutoPost();
            cache.post_id = post.postId;
            string postData = JsonConvert.SerializeObject(cache);
            Dictionary<string, StringValues> values = new Dictionary<string, StringValues>();
            values.Add("post", postData);
            IFormCollection collection = new FormCollection(values);
            var success = controller.AddFiles(files, collection);
            var unsuccess = controller.AddFiles(null, collection);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void UpdateOrder()
        {
            AutoPost post = CreateAutoPost();
            cache.post_id = post.postId;
            cache.order = 7;
            cache.file_id = post.files.First().fileId;
            var success = controller.UpdateOrder(cache);
            cache.post_id = 0;
            var unsuccess = controller.UpdateOrder(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
        [Test]
        public void DeleteFile()
        {
            AutoPost post = CreateAutoPost();
            cache.post_id = post.postId;
            cache.file_id = post.files.First().fileId;
            var success = controller.DeleteFile(cache);
            cache.post_id = 0;
            var unsuccess = controller.DeleteFile(cache);
            Assert.AreEqual(success.Value.success, true);
            Assert.AreEqual(unsuccess.Value.success, false);
        }
    }
}
