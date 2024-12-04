namespace Domain.Admins
{
    public interface IBlogPostRepository
    {
        void Add(BlogPost blogPost);
        void Delete(BlogPost blogPost);
        void Update(BlogPost blogPost);
        BlogPost[] GetPosts(int since, int count);
        BlogPost GetPost(int postId);
        BlogPost GetBy(int postId, bool deleted = false);
    }
}
