namespace Domain.GettingSubscribes
{
    public interface ITaskGettingSubscribesRepository
    {
        void Update(ICollection<TaskGS> tasks);
        ICollection<TaskGS> GetBy(long accountId);
    }
}
