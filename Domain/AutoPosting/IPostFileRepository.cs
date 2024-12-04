namespace Domain.AutoPosting
{
    public interface IPostFileRepository
    {
        void Create(PostFile postFile);
        void Update(PostFile postFile);
        void UpdateRange(ICollection<PostFile> posts);
        PostFile GetBy(long postId);
        PostFile GetBy(long fileId, long postId, bool fileDeleted = false);
        ICollection<PostFile> GetBy(long postId, bool fileDeleted = false);
    }
}
