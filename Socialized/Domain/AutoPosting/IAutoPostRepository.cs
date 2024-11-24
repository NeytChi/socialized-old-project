namespace Domain.AutoPosting
{
    public interface IAutoPostRepository
    {
        void Add(AutoPost autoPost);
        void Update(AutoPost autoPost);
        List<AutoPost> GetBy(DateTime executeAt, bool postExecuted = false, bool postDeleted = false);
        List<AutoPost> GetBy(DateTime deleteAfter, bool autoDeleted = false,
            bool postExecuted = true,
            bool postAutoDeleted = false,
            bool postDeleted = false);
    }
}
