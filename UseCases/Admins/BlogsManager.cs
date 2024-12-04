using Serilog;
using Domain.Admins;

namespace UseCases.Admins
{
    public class BlogsManager
    {
        private IBlogPostRepository Repository;
        public ILogger Logger;

        public BlogsManager(ILogger logger, IBlogPostRepository repository)
        {
            Logger = logger;
            Repository = repository;
        }
        public BlogPost CreatePost(BlogCache cache, ref string message)
        {
            if (SubjectIsTrue(cache.post_subject, ref message)
                && HtmlTextIsTrue(cache.post_htmltext, ref message)
                && LanguageIsTrue(cache.post_language, ref message))
            {
                var post = new BlogPost()
                {
                    postSubject = cache.post_subject,
                    postHtmlText = cache.post_htmltext,
                    postLanguage = cache.post_language,
                    adminId = cache.admin_id,
                    createdAt = DateTime.Now
                };
                Repository.Add(post);
                Logger.Information("Create a new blog post, id -> " + post.postId);
                return post;
            }
            return null;
        }
        public dynamic[] GetPosts(int since, int count)
        {
            Logger.Information("Get posts, since -> " + since + " count -> " + count);
            return Repository.GetPosts(since, count);
        }
        public BlogPost UpdatePost(BlogCache cache, ref string message)
        {
            var post = Repository.GetPost(cache.post_id);
            if (post != null)
            {
                if (LanguageIsTrue(cache.post_language, ref message))
                    post.postLanguage = cache.post_language;
                if (SubjectIsTrue(cache.post_subject, ref message))
                    post.postSubject = cache.post_subject;
                if (HtmlTextIsTrue(cache.post_htmltext, ref message))
                    post.postHtmlText = cache.post_htmltext;
                post.updatedAt = DateTime.Now;
                Repository.Update(post);
                Logger.Information("Blog post was updated, id -> " + post.postId);
            }
            else
            {
                message = "Undefined post id.";
            }
            return post;
        }
        public bool DeletePost(int postId, ref string message)
        {
            var post = Repository.GetPost(postId);
            if (post != null)
            {
                post.deleted = true;
                Repository.Update(post);
                Logger.Information("Blog post was deleted, id -> " + post.postId);
                return true;
            }
            else
            {
                message = "Unknow post id.";
            }
            return false;
        }
        public bool SubjectIsTrue(string subject, ref string message)
        {
            if (!string.IsNullOrEmpty(subject))
            {
                if (subject.Length > 0 && subject.Length < 255)
                {
                    return true;
                }
                message = "Subject length required more than 0 characters & less that 255.";
            }
            else
            {
                message = "Subject is null or empty.";
            }
            return false;
        }
        public bool HtmlTextIsTrue(string htmlText, ref string message)
        {
            if (!string.IsNullOrEmpty(htmlText))
            {
                return true;
            }
            else
            {
                message = "Html text is null or empty.";
            }
            return false;
        }
        public bool LanguageIsTrue(int language, ref string message)
        {
            switch ((BlogLanguage)language)
            {
                case BlogLanguage.English:
                case BlogLanguage.Russian:
                    return true;
                default:
                    message = "You pick language that server doesn't support. Use only english(1) or russian(2).";
                    return false;
            }
        }
    }
}