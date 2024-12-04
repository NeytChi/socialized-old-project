using Domain.Admins;
using Microsoft.Extensions.Logging;

namespace Infrastructure
{
    public class BlogPostRepository
    {
        private Context _context;
        public BlogPostRepository(Context context)
        {
            _context = context;
        }
        public BlogPost[] GetPosts(int since, int count)
        {
            return (from post in _context.BlogPosts
                    join admin in _context.Admins on post.adminId equals admin.adminId
                    where post.deleted == false
                    orderby post.postId descending
                    select post)
            .Skip(since * count)
            .Take(count)
            .ToArray();
        }
        public BlogPost GetPost(int postId)
        {
            return (from post in _context.BlogPosts
                join admin in _context.Admins on post.adminId equals admin.adminId
                where post.deleted == false
                select post).FirstOrDefault();
        }
        public BlogPost GetBy(int postId, bool deleted = false)
        {
            return _context.BlogPosts.Where(b => b.postId == postId && b.deleted == deleted).FirstOrDefault();
        }
    }
}
