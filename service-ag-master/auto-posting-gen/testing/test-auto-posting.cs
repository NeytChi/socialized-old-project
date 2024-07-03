using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

using database.context;
using Models.Common;
using Models.AutoPosting;
using Models.SessionComponents;

using Managment;
using nautoposting;

using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;

namespace Testing.nautoposting
{
    [TestFixture]
    public class TestAutoPostingService
    {
        public Context context;
        public IGAccount account;
        public AutoPostingService service;
        public AutoPosting autoPosting;
        public AutoDeleting autoDeleting;
        public SessionManager manager;
        public TestAutoPostingService()
        {
            this.context = TestMockingContext.GetContext();           
            this.service = new AutoPostingService(false, context);
            this.manager = new SessionManager(context);
            User user = TestMockingContext.CreateUser();
            this.account = TestMockingContext.CreateSession(user.userId);
            this.autoPosting = new AutoPosting(service.s3UploadedFiles);
            this.autoDeleting = new AutoDeleting();
        }
        public AutoPost CreateAutoPost(long sessionId, bool postType)
        {
            DeleteAutoPost();
            AutoPost post = new AutoPost()
            {
                sessionId = sessionId,
                postType = postType,
                executeAt = DateTime.Now.AddMinutes(1),
                autoDelete = true,
                deleteAfter = DateTime.Now.AddMinutes(1).AddSeconds(15),
                postLocation = "Pacific Ocean",
                postDescription = "Good view)",
                postComment = "Realy nice to post this."
            };
            PostFile image = new PostFile
            {
                filePath = "auto-posts/202032/ocean",
                fileOrder = 1,
                fileType = false,
                createdAt = DateTime.Now
            };
            PostFile image2 = new PostFile
            {
                filePath = "auto-posts/202032/bbox",
                fileOrder = 2,
                fileType = false,
                createdAt = DateTime.Now
            };
            // PostFile video = new PostFile
            // {
            //     filePath = "/auto-posts/20191111/ocean-from-waterbike",
            //     fileOrder = 2,
            //     fileType = true,
            //     createdAt = DateTime.Now
            // };
            post.files.Add(image);
            post.files.Add(image2);
            //post.files.Add(video);
            context.AutoPosts.Add(post);
            context.SaveChanges();
            return post;
        }
        public void DeleteAutoPost()
        {
            AutoPost post = context.AutoPosts.Where(a 
                => a.sessionId == account.accountId).FirstOrDefault();
            if (post != null) {
                context.AutoPosts.Remove(post);
                context.SaveChanges();
            }
        }
        [Test]
        public void CheckToRun()
        {
            service.CheckToRun();  
        }
        [Test]
        public void GetAutoPosts()
        {
            DeleteAutoPost();
            ICollection<AutoPost> emptyPosts = service.GetAutoPosts();
            CreateAutoPost(account.accountId, true);
            ICollection<AutoPost> posts = service.GetAutoPosts();
            Assert.AreEqual(emptyPosts.Count, 0);
            Assert.AreEqual(posts.Count, 1);
        }
        [Test]
        public void GetPostFiles()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            ICollection<PostFile> success = service.GetPostFiles(post.postId);
            Assert.AreEqual(success.Count, 1);
            ICollection<PostFile> unsuccess = service.GetPostFiles(12345);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void StartAutoPosts()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            ICollection<AutoPost> posts = new List<AutoPost>();
            service.StartAutoPosts(posts);
        }
        [Test]
        public void PerformAutoPost()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            autoPosting.PerformAutoPost(post);
        }
        [Test]
        public void RouteAutoPost()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            bool success = autoPosting.RouteAutoPost(post);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void PerformPost()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            Session session = manager.LoadSession(post.sessionId);
            bool success = autoPosting.PerformPost(post, session);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void CreatePost()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            Session session = manager.LoadSession(post.sessionId);
            var album = autoPosting.CreateAlbum(post.files);
            string success = autoPosting.CreatePost(ref session, album, "Good view", null);
            Assert.AreNotEqual(success, null);
        }
        [Test]
        public void CreateComment()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            Session session = manager.LoadSession(post.sessionId);
            var album = autoPosting.CreateAlbum(post.files);
            string mediaId = autoPosting.CreatePost(ref session, album, "Good view", null);
            bool success = autoPosting.CreateComment(ref session, mediaId, "Glad to see this.");
            Assert.AreEqual(success, true);
        }
        [Test]
        public void GetLocationShort()
        {
            Session session = manager.LoadSession(account.accountId);
            var success = autoPosting.GetLocationShort(ref session, "London");
            Assert.AreNotEqual(success, null);
        }
        [Test]
        public void CreateAlbum()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            var success = autoPosting.CreateAlbum(post.files);
            Assert.AreEqual(success.Length, post.files.Count);
        }
        [Test]
        public void PerformStories()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            Session session = manager.LoadSession(post.sessionId);
            bool success = autoPosting.PerformStories(post, session);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void CreateStories()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            Session session = manager.LoadSession(post.sessionId);
            string success = autoPosting.CreateStories(ref session, post.files.First());
            Assert.AreNotEqual(success, null);
        }
        [Test]
        public void CreateStoryImage()
        {
            WebClient client = new WebClient();
            AutoPost post = CreateAutoPost(account.accountId, false);
            Session session = manager.LoadSession(post.sessionId);
            InstaImage image = new InstaImage() { 
                ImageBytes = client.DownloadData(service.s3UploadedFiles + post.files.First().filePath),
                Height = 0, Width = 0
            };
            string success = autoPosting.CreateStoryImage(ref session, image);
            Assert.AreNotEqual(success, null);
        }
        [Test]
        public void CreateStoryVideo()
        {
            DeleteAutoPost();
            AutoPost post = new AutoPost()
            {
                sessionId = account.accountId,
                postType = false,
                executeAt = DateTime.Now.AddMinutes(1),
                autoDelete = true,
                deleteAfter = DateTime.Now.AddMinutes(1).AddSeconds(15),
                postLocation = "Pacific Ocean",
                postDescription = "Good view)",
                postComment = "Realy nice to post this."
            };
            PostFile video = new PostFile
            {
                filePath = "/auto-posts/202032/ocean-from-waterbike",
                fileOrder = 1,
                fileType = true,
                createdAt = DateTime.Now
            };
            post.files.Add(video);
            context.AutoPosts.Add(post);
            context.SaveChanges();
            Session session = manager.LoadSession(post.sessionId);
            InstaVideoUpload videoUpload = new InstaVideoUpload();
            videoUpload.Video = new InstaVideo(Directory.GetCurrentDirectory()
                + post.files.First().filePath, 0, 0);
            videoUpload.VideoThumbnail = new InstaImage(Directory.GetCurrentDirectory()
                + "/Black", 0, 0);
            string success = autoPosting.CreateStoryVideo(ref session, videoUpload);
            Assert.AreNotEqual(success, null);
        }
        [Test]
        public void GetAutoDelete()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            ICollection<AutoPost> unsuccess = service.GetAutoDelete();
            post.postExecuted = true;
            context.AutoPosts.Update(post);
            context.SaveChanges();
            ICollection<AutoPost> success = service.GetAutoDelete();
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess.Count, 0);
        }
        [Test]
        public void StartAutoDelete()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            post.postExecuted = true;
            context.AutoPosts.Update(post);
            context.SaveChanges();
            ICollection<AutoPost> posts = new List<AutoPost>();
            service.StartAutoDelete(posts);
        }
        [Test]
        public void PerformAutoDelete()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            Session session = manager.LoadSession(post.sessionId);
            autoPosting.PerformStories(post, session);
            post.files = context.PostFiles.Where(p 
                => p.postId == post.postId).ToList();
            autoDeleting.PerformAutoDelete(post);
        }
        [Test]
        public void RouteAutoDelete()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            Session session = manager.LoadSession(post.sessionId);
            autoPosting.PerformStories(post, session);
            post.files = context.PostFiles.Where(p 
                => p.postId == post.postId).ToList();
            bool success = autoDeleting.RouteAutoDelete(post, session);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void PerformDeletePost()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            Session session = manager.LoadSession(post.sessionId);
            autoPosting.PerformStories(post, session);
            post.files = context.PostFiles.Where(p 
                => p.postId == post.postId).ToList();
            bool success = autoDeleting.PerformDeletePost(post,ref session);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void PerformDeleteStories()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            Session session = manager.LoadSession(post.sessionId);
            autoPosting.PerformStories(post, session);
            post.files = context.PostFiles.Where(p 
                => p.postId == post.postId).ToList();
            bool success = autoDeleting.PerformDeleteStories(post,ref session);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void DeletePost()
        {
            AutoPost post = CreateAutoPost(account.accountId, true);
            Session session = manager.LoadSession(post.sessionId);
            autoPosting.PerformPost(post, session);
            post.files = context.PostFiles.Where(p 
                => p.postId == post.postId).ToList();
            PostFile file = post.files.First(); 
            bool success = autoDeleting.DeletePost(file, ref session);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void DeleteStory()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            Session session = manager.LoadSession(post.sessionId);
            autoPosting.PerformStories(post, session);
            post.files = context.PostFiles.Where(p 
                => p.postId == post.postId).ToList();
            PostFile file = post.files.First(); 
            bool success = autoDeleting.DeleteStory(ref session, file);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void EndAutoPost()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            autoPosting.EndAutoPost(post);
        }
        [Test]
        public void EndAutoDelete()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            autoDeleting.EndAutoDelete(post);
        }
        [Test]
        public void UpdatePostMediaId()
        {
            AutoPost post = CreateAutoPost(account.accountId, false);
            autoPosting.UpdatePostMediaId(post.files.First(), "1");
        }
    }
}