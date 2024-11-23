using Serilog;
using NUnit.Framework;

using Managment;
using socialized;
using Controllers;
using database.context;
using Models.AdminPanel;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestBlogController
    {
        Context context;
        BlogController controller;

        public TestBlogController()
        {
            AuthOptions authOptions = new AuthOptions();
            this.context = TestMockingContext.GetContext();
            this.controller = new BlogController(this.context);
            this.controller.log = new LoggerConfiguration().CreateLogger();
        }
        [Test]
        public void CreatePost()
        {
            Admin admin = TestMockingContext.CreateAdmin();
            BlogCache cache = new BlogCache() {
                admin_id = admin.adminId,
                post_subject = TestMockingContext.values.post_subject,
                post_htmltext = TestMockingContext.values.post_htmltext,
                post_language = TestMockingContext.values.post_language
            };
            var result = controller.CreatePost(cache);
            Assert.AreEqual(result.Value.success, true);
        }
        [Test]
        public void GetBlogs()
        {
            BlogPost post = TestMockingContext.CreateBlogPostEnviroment();
            Assert.AreEqual(controller.GetBlogs(0, 1).Value.success, true);
        }
        [Test]
        public void GetBlog()
        {
            BlogPost post = TestMockingContext.CreateBlogPostEnviroment();
            Assert.AreEqual(controller.GetBlog(post.postId).Value.success, true);
        }
        [Test]
        public void DeleteBlogPost()
        {
            BlogPost post = TestMockingContext.CreateBlogPostEnviroment();
            BlogCache cache = new BlogCache() {
                post_id = post.postId
            };
            Assert.AreEqual(controller.DeletePost(cache).Value.success, true);
        }
    }
}