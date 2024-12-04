namespace Domain.AutoPosting
{
    public interface IPostFileRepository
    {
        void Create(AutoPostFile postFile);
        void Update(AutoPostFile postFile);
        void UpdateRange(ICollection<AutoPostFile> posts);
        AutoPostFile GetBy(long postId);
        AutoPostFile GetBy(long fileId, long postId, bool fileDeleted = false);
        ICollection<AutoPostFile> GetBy(long postId, bool fileDeleted = false);
    }
}
