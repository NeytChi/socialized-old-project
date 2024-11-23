namespace Domain.AutoPosting
{
    public interface IAutoPostRepository
    {
        void Add(AutoPost autoPost);
        void Update(AutoPost autoPost);
    }
}
