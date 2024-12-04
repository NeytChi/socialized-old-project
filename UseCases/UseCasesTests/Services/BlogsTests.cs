using Serilog;
using NUnit.Framework;

using Managment;
using socialized;
using UseCasesTasks;
using database.context;
using Models.AdminPanel;


namespace UseCases.Services.Tests
{
    [TestFixture]
    public class BlogsTests
    {
        Blogs blogs;
        string error;
        Context context;
            
        public BlogsTests()
        {
            AuthOptions authOptions = new AuthOptions();
            this.context = MockingContextTests.GetContext();
            this.blogs = new Blogs(new LoggerConfiguration().CreateLogger(), context);
        }
        [Test]
        public void CreatePost()
        {
            Admin admin = MockingContextTests.CreateAdmin();
            BlogCache cache = new BlogCache() {
                admin_id = admin.adminId,
                post_subject = MockingContextTests.values.post_subject,
                post_htmltext = MockingContextTests.values.post_htmltext,
                post_language = MockingContextTests.values.post_language
            };
            BlogPost post = blogs.CreatePost(cache, ref error);
            Assert.AreEqual(post.adminId, admin.adminId);
        }
        [Test]
        public void SubjectIsTrue()
        {
            Assert.AreEqual(blogs.SubjectIsTrue(MockingContextTests.values.post_subject, ref error), true);
            Assert.AreEqual(blogs.SubjectIsTrue("", ref error), false);
            Assert.AreEqual(blogs.SubjectIsTrue(null, ref error), false);
        }
        [Test]
        public void HtmlTextIsTrue()
        {
            Assert.AreEqual(blogs.HtmlTextIsTrue(MockingContextTests.values.post_subject, ref error), true);
            Assert.AreEqual(blogs.HtmlTextIsTrue("", ref error), false);
            Assert.AreEqual(blogs.HtmlTextIsTrue(null, ref error), false);
        }
        [Test]
        public void LanguageIsTrue()
        {
            Assert.AreEqual(blogs.LanguageIsTrue(1, ref error), true);
            Assert.AreEqual(blogs.LanguageIsTrue(2, ref error), true);
            Assert.AreEqual(blogs.LanguageIsTrue(3, ref error), false);
        }
        [Test]
        public void GetNonDelete()
        {
            Admin admin = MockingContextTests.CreateAdmin();
            Assert.AreEqual(blogs.GetNonDelete(admin.adminEmail, ref error).adminId, admin.adminId);
        }
        [Test]
        public void GetNonDelete_Not_Exists()
        {
            MockingContextTests.DeleteExistAdmin(MockingContextTests.values.admin_email);
            Assert.AreEqual(blogs.GetNonDelete(MockingContextTests.values.admin_email, ref error), null);
        }
        [Test]
        public void GetPosts()
        {
            BlogPost post = MockingContextTests.CreateBlogPostEnviroment();
            dynamic[] posts = blogs.GetPosts(0, 1);
            Assert.AreEqual(posts.Length, 1);
        }
        [Test]
        public void GetPosts_Deleted()
        {
            BlogPost post = MockingContextTests.CreateBlogPostEnviroment();
            post.deleted = true;
            context.BlogPosts.Update(post);
            context.SaveChanges();
            dynamic[] posts = blogs.GetPosts(0, 0);
            Assert.AreEqual(posts.Length, 0);
        }
        [Test]
        public void GetPost()
        {
            BlogPost post = MockingContextTests.CreateBlogPostEnviroment();
            dynamic outPost = blogs.GetPost(post.postId, ref error);
            Assert.AreEqual(outPost.post_id, post.postId);
        }
        [Test]
        public void GetPost_Deleted()
        {
            BlogPost post = MockingContextTests.CreateBlogPostEnviroment();
            post.deleted = true;
            context.BlogPosts.Update(post);
            context.SaveChanges();
            dynamic outPost = blogs.GetPost(post.postId, ref error);
            Assert.AreEqual(outPost, null);
        }
        [Test]
        public void DeleteBlogPost()
        {
            BlogPost post = MockingContextTests.CreateBlogPostEnviroment();
            Assert.AreEqual(blogs.DeletePost(post.postId, ref error), true);
        }
        [Test]
        public void DeleteBlogPost_Not_Exist()
        {
            BlogPost post = MockingContextTests.CreateBlogPostEnviroment();
            post.deleted = true;
            context.BlogPosts.Update(post);
            context.SaveChanges();
            Assert.AreEqual(blogs.DeletePost(post.postId, ref error), false);
        }
        [Test]
        public void UpdateBlogPost()
        {
            BlogPost post = MockingContextTests.CreateBlogPostEnviroment();
            BlogCache cache = new BlogCache() {
                post_id = post.postId,
                post_subject = MockingContextTests.values.post_subject,
                post_htmltext = MockingContextTests.values.post_htmltext,
                post_language = MockingContextTests.values.post_language
            };
            Assert.AreEqual(blogs.UpdatePost(cache, ref error).postId, post.postId);
        }
        [Test]
        public void UpdateBlogPost_Not_Exist()
        {
            BlogPost post = MockingContextTests.CreateBlogPostEnviroment();
            post.deleted = true;
            context.BlogPosts.Update(post);
            context.SaveChanges();
            BlogCache cache = new BlogCache() {
                post_id = post.postId,
                post_subject = MockingContextTests.values.post_subject,
                post_htmltext = MockingContextTests.values.post_htmltext,
                post_language = MockingContextTests.values.post_language
            };
            Assert.AreEqual(blogs.UpdatePost(cache, ref error), null);
        }
    }
}