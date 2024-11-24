using System;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using NUnit.Framework;
using Microsoft.AspNetCore.Http;

using Managment;
using database.context;
using Models.Common;
using Models.AutoPosting;
using Models.SessionComponents;

namespace UseCases.Services.Tests
{
    [TestFixture]
    public class AutoPostingManagerTests
    {
        public AutoPostingManager manager;
        public Context context;
        public User user;
        public IGAccount account;
        public List<IFormFile> files = new List<IFormFile>();
        public AutoPostCache cache;
        public string message;
        
        public AutoPostingManagerTests()
        {
            this.context = MockingContextTests.GetContext();
            this.manager = new AutoPostingManager(new LoggerConfiguration().CreateLogger(), context);
            user = MockingContextTests.CreateUser();
            account = MockingContextTests.CreateSession(user.userId);
            IFormFile file = MockingContextTests.CreateFile();
            files.Add(file);
            files.Add(file);
            this.cache = CreateObjectPost();   
            this.cache.user_token = user.userToken;
            this.cache.session_id = account.accountId;
            this.cache.files = files;
        }
        public static AutoPostCache CreateObjectPost()
        {
            return new AutoPostCache()
            {
                post_type = true,
                execute_at = DateTime.Now.AddDays(2),
                location = "Kiev",
                auto_delete = true,
                delete_after = DateTime.Now.AddDays(3),
                description = "Hello, big world!",
                comment = "How are you?",
                count = 10,
                from = DateTime.UtcNow,
                to = DateTime.UtcNow.AddMonths(5),
            };
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
                deleteAfter = DateTime.Now.AddMonths(3),
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
        public void DeleteAutoPost()
        {
            AutoPost[] posts = context.AutoPosts.ToArray();
            context.AutoPosts.RemoveRange(posts);
            context.SaveChanges();
        }
        [Test]
        public void CreatePost()
        {
            bool success = manager.CreatePost(cache, ref message);
            cache.files = null;
            bool unsuccess = manager.CreatePost(cache, ref message);
            cache.files = files;
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void SaveAutoPost()
        {
            manager.SaveAutoPost(cache, new List<PostFile>());
        }
        [Test]
        public void CheckFileType()
        {
            Assert.AreEqual(manager.CheckFileType("image/jpeg", ref message), true);
            Assert.AreEqual(manager.CheckFileType("video/mp4", ref message), true);
            Assert.AreEqual(manager.CheckFileType("test", ref message), false);
        }
        [Test]
        public void CheckAutoPost()
        {
            cache.location = null;
            bool successPost = manager.CheckAutoPost(cache,ref message);
            cache.post_type = false;
            bool successStories = manager.CheckAutoPost(cache,ref message);
            cache.files = files;
            
            Assert.AreEqual(successPost, true);
            Assert.AreEqual(successStories, true);
        }
        [Test]
        public void CheckStories()
        {
            bool successStories = manager.CheckStories(cache,ref message);
            cache.files = null;
            bool unsuccess = manager.CheckAutoPost(cache, ref message);
            cache.files = files;
            Assert.AreEqual(successStories, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckPost()
        {
            Assert.AreEqual(manager.CheckPost(cache,ref message), true);
        }
        [Test]
        public void CheckExecuteTime()
        {
            bool success = manager.CheckExecuteTime(DateTime.Now.AddSeconds(5), 0, ref message);
            bool unsuccess = manager.CheckExecuteTime(DateTime.Now.AddSeconds(-10), 0, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckLocation()
        {
            bool success = manager.CheckLocation("Kiev", account.accountId, ref message);
            bool unsuccess = manager.CheckLocation("", account.accountId, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckDescription()
        {
            bool success = manager.CheckDescription("Hello world!", ref message);
            bool unsuccess = manager.CheckDescription(null, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckComment()
        {
            bool success = manager.CheckComment("Hello world!", ref message);
            bool unsuccess = manager.CheckComment(null, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckFiles()
        {
            bool success = manager.CheckFiles(files, ref message);
            bool unsuccess = manager.CheckFiles(null, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckDeleteAfter()
        {
            bool success = manager.CheckDeleteAfter(true, DateTime.Now.AddDays(2), DateTime.Now.AddDays(1), ref message);
            bool successWithout = manager.CheckDeleteAfter(false, DateTime.Now.AddDays(2), DateTime.Now.AddDays(1), ref message);
            bool unsuccess = manager.CheckDeleteAfter(null, DateTime.Now.AddDays(2), DateTime.Now.AddDays(1), ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(successWithout, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void StartStop()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            bool successStart = manager.StartStop(cache, ref message);
            bool successStop = manager.StartStop(cache, ref message);
            cache.post_id = 0;
            bool unsuccess = manager.StartStop(cache, ref message);
            Assert.AreEqual(successStart, true);
            Assert.AreEqual(successStop, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetNonDeletedPost()
        {
            AutoPost autoPost = CreateAutoPost();
            AutoPost success = manager.GetNonDeletedPost(user.userToken, autoPost.postId, ref message);
            AutoPost unsuccess = manager.GetNonDeletedPost(null, autoPost.postId, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetByCategory()
        {
            cache.category = 1;
            var successPublished = manager.GetByCategory(cache, ref message);
            cache.category = 2;
            var successPast = manager.GetByCategory(cache, ref message);
            cache.category = 3;
            var successDeleted = manager.GetByCategory(cache, ref message);
            cache.category = 4;
            var successUnpublished = manager.GetByCategory(cache, ref message);
            Assert.AreNotEqual(successPublished, null);
            Assert.AreNotEqual(successPast, null);
            Assert.AreNotEqual(successDeleted, null);
            Assert.AreNotEqual(successUnpublished, null);
        }
        [Test]
        public void UpdateAutoPost()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            bool success = manager.UpdateAutoPost(cache, ref message);
            cache.user_token = user.userToken;
            Assert.AreEqual(success, true);
        }
        [Test]
        public void UpdateStories()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            cache.delete_after = autoPost.deleteAfter;
            cache.execute_at = autoPost.executeAt;
            bool success = manager.UpdateStories(autoPost, cache, ref message);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void UpdatePost()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            bool success = manager.UpdatePost(autoPost, cache, ref message);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void UpdateCommonPost()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            manager.UpdateCommonPost(ref autoPost, cache);
        }
        [Test]
        public void CheckToUpdateExecuteTime()
        {
            bool success = manager.CheckToUpdateExecuteTime(DateTime.Now.AddSeconds(5), 0, ref message);
            bool unsuccess = manager.CheckToUpdateExecuteTime(DateTime.Now.AddSeconds(-10), 0, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckToUpdateDeleteAfter()
        {
            bool success = manager.CheckToUpdateDeleteAfter(true, DateTime.Now.AddDays(2), DateTime.Now.AddDays(1), 0, ref message);
            bool optional = manager.CheckToUpdateDeleteAfter(null, DateTime.Now.AddDays(2), DateTime.Now.AddDays(1), 0, ref message);
            bool unsuccess = manager.CheckToUpdateDeleteAfter(true, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2), 0, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(optional, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckToUpdateLocation()
        {
            bool success = manager.CheckToUpdateLocation("Kiev", account.accountId, ref message);
            bool successOptional = manager.CheckToUpdateLocation(null, account.accountId, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(successOptional, true);
        }
        [Test]
        public void CheckToUpdateDescription()
        {
            bool success = manager.CheckToUpdateDescription("Kiev", ref message);
            bool successOptional = manager.CheckToUpdateDescription(null, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(successOptional, true);
        }
        [Test]
        public void CheckToUpdateComment()
        {
            bool success = manager.CheckToUpdateComment("Kiev", ref message);
            bool successOptional = manager.CheckToUpdateComment(null, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(successOptional, true);
        }
        [Test]
        public void UpdateExecuteTime()
        {
            AutoPost post = CreateAutoPost();
            manager.UpdateExecuteTime(ref post, DateTime.Now.AddHours(2), 2);
        }
        [Test]
        public void UpdateLocation()
        {
            AutoPost post = CreateAutoPost();
            manager.UpdateLocation(ref post, "Kiev");
        }
        [Test]
        public void UpdateDeleteAfter()
        {
            AutoPost post = CreateAutoPost();
            manager.UpdateDeleteAfter(ref post, true, DateTime.Now.AddDays(3), 3);
        }
        [Test]
        public void CheckToUpdateTimezone()
        {
            Assert.AreEqual(manager.CheckToUpdateTimezone(0, ref message), true);
            Assert.AreEqual(manager.CheckToUpdateTimezone(5, ref message), true);
            Assert.AreEqual(manager.CheckToUpdateTimezone(-15, ref message), false);
        }
        [Test]
        public void UpdateDescription()
        {
            AutoPost post = CreateAutoPost();
            manager.UpdateDescription(ref post, "Hello world!");
        }
        [Test]
        public void UpdateComment()
        {
            AutoPost post = CreateAutoPost();
            manager.UpdateComment(ref post, "Hello world!");
        }
        [Test]
        public void Delete()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            bool success = manager.Delete(cache, ref message);
            cache.user_token = null;
            bool unsuccess = manager.Delete(cache, ref message);
            cache.user_token = user.userToken;
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetPosts()
        {
            AutoPost autoPost = CreateAutoPost();
            Assert.AreEqual(manager.GetPosts(cache, false, false).Count, 1);
        }
        [Test]
        public void GetUnpublishedPosts()
        {
            AutoPost autoPost = CreateAutoPost();
            autoPost.executeAt = DateTime.UtcNow;
            context.AutoPosts.Update(autoPost);
            context.SaveChanges();
            Assert.AreEqual(manager.GetUnpublishedPosts(cache).Count, 1);
        }
        [Test]
        public void AddFilesToPost()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            List<PostFile> success = manager.AddFilesToPost(cache, ref message);
            cache.post_id = 0;
            List<PostFile> unsuccess = manager.AddFilesToPost(cache, ref message);
            Assert.AreEqual(success.Count, cache.files.Count);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void DeletePostFile()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.post_id = autoPost.postId;
            PostFile file = autoPost.files.ToList()[0];
            cache.file_id = file.fileId;
            bool success = manager.DeletePostFile(cache, ref message);
            cache.post_id = 0;
            bool unsuccess = manager.DeletePostFile(cache, ref message);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetNonDeleteFile()
        {
            AutoPost autoPost = CreateAutoPost();
            PostFile file = autoPost.files.ToList()[0];
            var success = manager.GetNonDeleteFile(file.fileId, autoPost.postId, ref message);
            var unsuccess = manager.GetNonDeleteFile(0, autoPost.postId, ref message);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void UpdateOrderFile()
        {
            AutoPost autoPost = CreateAutoPost();
            cache.user_token = autoPost.account.User.userToken;
            cache.post_id = autoPost.postId;
            cache.files_id = autoPost.files.OrderByDescending(x => x.fileId)
                .Select(x => x.fileId).ToList();
            Assert.AreEqual(manager.UpdateOrderFile(cache, ref message), true);
            cache.post_id = 0;
            Assert.AreEqual(manager.UpdateOrderFile(cache, ref message), false);
        }
        [Test]
        public void GetNonExecutedPost()
        {
            AutoPost autoPost = CreateAutoPost();
            AutoPost success = manager.GetNonExecutedPost(user.userToken, autoPost.postId, ref message);
            Assert.AreNotEqual(success, null);
        }
        [Test]
        public void ResortFiles()
        {
            AutoPost autoPost = CreateAutoPost();
            PostFile file = autoPost.files.ToList()[0];
            PostFile secondFile = new PostFile()
            {
                postId = autoPost.postId,
                fileOrder = 2,
                filePath = "/test/"
            };
            context.PostFiles.Add(secondFile);
            context.PostFiles.Remove(file);
            context.SaveChanges();
            manager.ResortFiles(autoPost.files, 1);
        }
        [Test]
        public void FilesIdIsTrue()
        {
            List<PostFile> files = new List<PostFile>();
            List<long> filesId = new List<long>();
            for (int i = 1; i < 6 ; i++)
                filesId.Add(i);
            for (int i = 5; i > 0 ; i--)
                files.Add(new PostFile() { fileId = i });
            Assert.AreEqual(manager.FilesIdIsTrue(files, filesId, ref message), true);
        }
        [Test]
        public void FilesIdIsTrue_Not_Same_Count()
        {
            List<PostFile> files = new List<PostFile>();
            List<long> filesId = new List<long>();
            for (int i = 0; i < 5 ; i++)
                filesId.Add(i);
            for (int i = 3; i > 0; i--)
                files.Add(new PostFile() { fileId = i });
            Assert.AreEqual(manager.FilesIdIsTrue(files, filesId, ref message), false);
        }
       [Test]
        public void FilesIdIsTrue_Not_Same_File_Id()
        {
            List<PostFile> files = new List<PostFile>();
            List<long> filesId = new List<long>();
            for (int i = 0; i < 5 ; i++)
                filesId.Add(i);
            for (int i = 8; i > 3; i--)
                files.Add(new PostFile() { fileId = i });
            Assert.AreEqual(manager.FilesIdIsTrue(files, filesId, ref message), false);
        }
        [Test]
        public void UploadedTextIsTrue()
        {
            Assert.AreEqual(manager.UploadedTextIsTrue("#summer #sun go-to shop with @eric_andrea", ref message), true);
            Assert.AreEqual(manager.UploadedTextIsTrue(null, ref message), true);
            Assert.AreEqual(manager.UploadedTextIsTrue("", ref message), true);
        }
        [Test]
        public void HashtagsIsTrue()
        {
            string hashtag = "#summer ";
            Assert.AreEqual(manager.HashtagsIsTrue(hashtag, ref message), true);
        }
        [Test]
        public void HashtagsIsFalse()
        {
            string hashtag = "#summer ", uploaded = string.Empty;
            for (int i = 0; i < manager.availableHashtags + 1; i++)
                uploaded += hashtag;
            Assert.AreEqual(manager.HashtagsIsTrue(uploaded, ref message), false);
        }
        [Test]
        public void TagsIsTrue()
        {
            string hashtag = "@summer ";
            Assert.AreEqual(manager.TagsIsTrue(hashtag, ref message), true);
        }
        [Test]
        public void TagsIsFalse()
        {
            string tag = "@summer ", uploaded = string.Empty;
            for (int i = 0; i < manager.availableTags + 1; i++)
                uploaded += tag;
            Assert.AreEqual(manager.TagsIsTrue(uploaded, ref message), false);
        }
        [Test]
        public void Recovery()
        {
            cache.user_token = user.userToken;
            AutoPost post = CreateAutoPost();
            cache.post_id = post.postId;
            cache.files_id = new List<long>();
            foreach(PostFile file in post.files)
                cache.files_id.Add(file.fileId);
            Assert.AreEqual(manager.Recovery(cache, ref message), true);
        }
        [Test]
        public void CheckCategory()
        {
            Category category = MockingContextTests.CreateCategory(account.accountId);
            Assert.AreEqual(manager.CheckCategory(account.accountId, category.categoryId, ref message), true);
        }
        [Test]
        public void CheckCategory_Empty()
        {
            Assert.AreEqual(manager.CheckCategory(account.accountId, 0, ref message), true);
        }
        [Test]
        public void CheckCategory_Unknow_Category()
        {
            Category category = MockingContextTests.CreateCategory(account.accountId);
            Assert.AreEqual(manager.CheckCategory(account.accountId, category.categoryId + 1, ref message), false);
        }
    }
}