using Domain.AutoPosting;

namespace Infrastructure
{
    public class CategoryRepository
    {
        private Context Context;
        public CategoryRepository(Context context)
        {
            Context = context;
        }
        public Category GetBy(long accountId, long categoryId, bool categoryDeleted = false)
        {
            return Context.Categories.Where(c => 
                c.accountId == accountId && 
                c.categoryId == categoryId && 
                c.categoryDeleted == categoryDeleted).FirstOrDefault();
        }
    }
}
