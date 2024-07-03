using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;

using Serilog;
using Serilog.Core;

using Common;
using socialized;
using database.context;
using Models.AdminPanel;

namespace Managment
{
    public class Blogs
    {
        private Context context;
        public Logger log;
        public Blogs(Logger log, Context context)
        {
            this.context = context;
            this.log = log;
        }
        public BlogPost CreatePost(BlogCache cache, ref string message)
        {
            if (SubjectIsTrue(cache.post_subject, ref message) 
                && HtmlTextIsTrue(cache.post_htmltext, ref message)
                && LanguageIsTrue(cache.post_language, ref message)) {
                BlogPost post = new BlogPost() {
                    postSubject = cache.post_subject,
                    postHtmlText = cache.post_htmltext,
                    postLanguage = cache.post_language,
                    adminId = cache.admin_id,
                    createdAt = DateTime.Now 
                };
                context.BlogPosts.Add(post);
                context.SaveChanges();
                log.Information("Create a new blog post, id -> " + post.postId);
                return post;
            }
            return null;
        }
        public dynamic[] GetPosts(int since, int count)
        {
            log.Information("Get posts, since -> " + since + " count -> " + count);
            return (from post in context.BlogPosts
            join admin in context.Admins on post.adminId equals admin.adminId
            where post.deleted == false
            orderby post.postId descending
            select new { 
                post_id = post.postId,
                post_subject = post.postSubject,
                post_language = post.postLanguage,
                created_at = post.createdAt,
                admin_fullname = admin.adminFullname
            })
            .Skip(since * count)
            .Take(count)
            .ToArray();
        }
        public dynamic GetPost(int postId, ref string message)
        {
            dynamic blogPost = (from post in context.BlogPosts
            join admin in context.Admins on post.adminId equals admin.adminId
            where post.deleted == false
            select new { 
                post_id = post.postId,
                post_subject = post.postSubject,
                post_htmltext = post.postHtmlText,
                post_language = post.postLanguage,
                created_at = post.createdAt,
                admin_fullname = admin.adminFullname
            }).FirstOrDefault();
            if (blogPost == null)
                message = "Unknow post id.";
            log.Information("Get post, id -> " + postId);
            return blogPost;
        }
        public BlogPost UpdatePost(BlogCache cache, ref string message)
        {
            BlogPost post;
            if ((post = GetNonDelete(cache.post_id, ref message)) != null) {
                if (LanguageIsTrue(cache.post_language, ref message)) 
                    post.postLanguage = cache.post_language;
                if (SubjectIsTrue(cache.post_subject, ref message)) 
                    post.postSubject = cache.post_subject;
                if (HtmlTextIsTrue(cache.post_htmltext, ref message)) 
                    post.postHtmlText = cache.post_htmltext;
                post.updatedAt = DateTime.Now;
                context.BlogPosts.Update(post);
                context.SaveChanges();
                log.Information("Blog post was updated, id -> " + post.postId);
            }
            return post;
        }
        public bool DeletePost(int postId, ref string message)
        {
            BlogPost post;
            if ((post = GetNonDelete(postId, ref message)) != null) {
                post.deleted = true;
                context.BlogPosts.Update(post);
                context.SaveChanges();
                log.Information("Blog post was deleted, id -> " + post.postId);
                return true;
            }
            return false;
        }
        public bool SubjectIsTrue(string subject, ref string message)
        {
            if (!string.IsNullOrEmpty(subject)) {
                if (subject.Length > 0 && subject.Length < 255)
                    return true;
                message = "Subject length required more than 0 characters & less that 255.";
            }
            else 
                message = "Subject is null or empty.";
            return false;
        }
        public bool HtmlTextIsTrue(string htmlText, ref string message)
        {
            if (!string.IsNullOrEmpty(htmlText)) {
                return true;
            }
            else 
                message = "Html text is null or empty.";
            return false;
        }
        public bool LanguageIsTrue(int language, ref string message)
        {
            switch ((BlogLanguage)language) {
                case BlogLanguage.English:
                case BlogLanguage.Russian:
                    return true;
                default: 
                    message = "You pick language that server doesn't support. Use only english(1) or russian(2).";
                    return false;
            }
        }
        public Admin GetNonDelete(string adminEmail, ref string message)
        {
            Admin admin = context.Admins.Where(a 
            => a.adminEmail == adminEmail 
            && a.deleted == false).FirstOrDefault();
            if (admin == null)
                message = "Unknow admin.";
            return admin;
        }
        public BlogPost GetNonDelete(int postId, ref string message)
        {
            BlogPost post = context.BlogPosts.Where(b 
            => b.postId == postId
            && b.deleted == false).FirstOrDefault();
            if (post == null)
                message = "Unknow post id.";
            else
                post.admin = context.Admins.Where(a => a.adminId == post.adminId).First();
            return post;
        }
    }
}