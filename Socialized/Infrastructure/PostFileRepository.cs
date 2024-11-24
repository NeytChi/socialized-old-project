using Domain.AutoPosting;

namespace Infrastructure
{
    public class PostFileRepository
    {
        private Context Context;
        public PostFileRepository(Context context)
        {
            Context = context;
        }
        public PostFile GetBy(long postId)
        {
            return Context.PostFiles.Where(p => p.postId == postId).FirstOrDefault();
        }
        public PostFile GetBy(long fileId, long postId, bool fileDeleted = false)
        {
            return Context.PostFiles.Where(f =>
                f.fileId == fileId &&
                f.postId == postId &&
                f.fileDeleted == fileDeleted)
                    .FirstOrDefault();
        }
        public ICollection<PostFile> GetBy(long postId, bool fileDeleted = false)
        {
            return Context.PostFiles.Where(f =>
                f.postId == postId &&
                f.fileDeleted == fileDeleted)
                .OrderBy(f => f.fileOrder).ToList();
        }
        public List<AutoPost> GetBy(
            DateTime deleteAfter,
            bool autoDeleted = false,
            bool postExecuted = true,
            bool postAutoDeleted = false,
            bool postDeleted = false)
        {
            return Context.AutoPosts.Where(a
                => a.autoDelete == autoDeleted
                && a.postExecuted == postExecuted
                && a.postAutoDeleted == postAutoDeleted
                && a.deleteAfter < deleteAfter
                && a.postDeleted == postDeleted
            ).OrderBy(a => a.deleteAfter).ToList();
        }
    }
}
