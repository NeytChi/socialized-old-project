using Domain.GettingSubscribes;

namespace UseCases.InstagramAccounts
{
    public interface ITaskGettingSubscribesRepository
    {
        void Update(ICollection<TaskGS> tasks);
        ICollection<TaskGS> GetBy(long accountId);
    }
}
