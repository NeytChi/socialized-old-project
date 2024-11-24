using Domain.AutoPosting;

namespace Infrastructure
{
    public class AutoPostRepository
    {
        private Context Context;
        public AutoPostRepository(Context context)
        {
            Context = context;
        }
        public List<AutoPost> GetBy(DateTime executeAt, 
            bool postExecuted = false, 
            bool postDeleted = false)
        {
            return Context.AutoPosts.Where(a => 
                a.postExecuted == postExecuted && 
                executeAt > a.executeAt && 
                a.postDeleted == postDeleted
                )
                .OrderBy(a => a.executeAt)
                .ToList();
        }
    }
}
