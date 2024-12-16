namespace Domain.AutoPosting
{
    public interface IAutoPostRepository
    {
        void Add(AutoPost autoPost);
        void Update(AutoPost autoPost);
        AutoPost GetByWithUserAndFiles(string userToken, long autoPostId, bool postDeleted = false);
        AutoPost GetByWithFiles(long autoPostFileId, bool postDeleted = false);
        AutoPost GetBy(string userToken, long postId, bool postDeleted = false);
        AutoPost GetBy(string userToken, long postId, bool postDeleted, bool postAutoDeleted, bool postExecuted);
        ICollection<AutoPost> GetBy(GetAutoPostsCommand command);
        List<AutoPost> GetBy(DateTime executeAt, bool postExecuted = false, bool postDeleted = false);
        List<AutoPost> GetBy(DateTime deleteAfter, bool autoDeleted = false,
            bool postExecuted = true,
            bool postAutoDeleted = false,
            bool postDeleted = false);
    }
}
