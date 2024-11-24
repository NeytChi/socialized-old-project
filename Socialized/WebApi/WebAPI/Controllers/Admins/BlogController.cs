using System;
using Serilog;
using System.Linq;
using Serilog.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Managment;
using database.context;
using Models.AdminPanel;

namespace WebAPI.Controllers.Admins
{
    [Route("v1.0/[controller]/[action]/")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        public BlogController(Context context)
        {
            blogs = new Blogs(log, context);
        }

        public Logger log = new LoggerConfiguration()
        .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
        .CreateLogger();

        public Blogs blogs;

        [HttpGet]
        [Authorize]
        [ActionName("CreatePost")]
        public ActionResult<dynamic> CreatePost(BlogCache cache)
        {
            string message = string.Empty;
            BlogPost post;

            cache.admin_id = int.Parse(HttpContext?.User.Claims.FirstOrDefault().Value ??
                cache.admin_id.ToString());
            if ((post = blogs.CreatePost(cache, ref message)) != null)
                return new
                {
                    success = true,
                    data = new
                    {
                        post_id = post.postId,
                        post_subject = post.postSubject,
                        post_language = post.postLanguage,
                        post_htmltext = post.postHtmlText,
                        created_at = post.createdAt,
                        admin_fullname = post.admin.adminFullname
                    }
                };
            return Return500Error(message);
        }
        [HttpGet]
        [ActionName("Blogs")]
        public ActionResult<dynamic> GetBlogs([FromQuery] int since, [FromQuery] int count)
        {
            string message = string.Empty;
            return new { success = true, data = new { posts = blogs.GetPosts(since, count) } };
        }
        [HttpGet]
        [ActionName("Blog")]
        public ActionResult<dynamic> GetBlog([FromQuery] int postId)
        {
            string message = string.Empty;

            dynamic post = blogs.GetPost(postId, ref message);
            if (post != null)
                return new { success = true, data = new { post } };
            return Return500Error(message);
        }
        [HttpPost]
        [Authorize]
        [ActionName("EditPost")]
        public ActionResult<dynamic> EditPost(BlogCache cache)
        {
            string message = string.Empty;

            BlogPost post;
            if ((post = blogs.UpdatePost(cache, ref message)) != null)
                return new
                {
                    success = true,
                    data = new
                    {
                        post_id = post.postId,
                        post_subject = post.postSubject,
                        post_language = post.postLanguage,
                        created_at = post.createdAt,
                        updated_at = post.updatedAt,
                        admin_fullname = post.admin.adminFullname,
                        post_htmltext = post.postHtmlText
                    }
                };
            return Return500Error(message);
        }
        [HttpPost]
        [Authorize]
        [ActionName("DeletePost")]
        public ActionResult<dynamic> DeletePost(BlogCache cache)
        {
            string message = string.Empty;

            if (blogs.DeletePost(cache.post_id, ref message))
                return new { success = true, message = "Blog post was deleted." };
            return Return500Error(message);
        }
        public dynamic Return500Error(string message)
        {
            if (Response != null)
                Response.StatusCode = 500;

            log.Warning(message + " IP -> " + HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new { success = false, message };
        }
    }
}
