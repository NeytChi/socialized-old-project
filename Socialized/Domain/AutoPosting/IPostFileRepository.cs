namespace Domain.AutoPosting
{
    public interface IPostFileRepository
    {
        void Create(PostFile postFile);
        void Update(PostFile postFile);
        PostFile GetBy(long postId);
        ICollection<PostFile> GetBy(long postId, bool fileDeleted = false);
    }
}
