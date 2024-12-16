using Domain.AutoPosting;

namespace UseCases.AutoPosts.AutoPostFiles
{
    public interface IAutoPostFileRepository
    {
        void Create(AutoPostFile postFile);
        void Create(ICollection<AutoPostFile> files);
        void Update(AutoPostFile file);
        void Update(ICollection<AutoPostFile> posts);
        AutoPostFile GetBy(long autoPostFileId, bool IsDeleted = false);
        AutoPostFile GetBy(long autoPostFileId, long autoPostId, bool fileDeleted = false);
        AutoPostFile GetBy(string userToken, long autoPostFileId, bool IsDeleted = false);
        ICollection<AutoPostFile> GetByRange(long autoPostId, bool fileDeleted = false);
    }
}
