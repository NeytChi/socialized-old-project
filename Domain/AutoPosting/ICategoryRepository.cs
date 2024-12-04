namespace Domain.AutoPosting
{
    public interface ICategoryRepository
    {
        Category GetBy(long accountId, long categoryId, bool categoryDeleted = false);
    }
}
