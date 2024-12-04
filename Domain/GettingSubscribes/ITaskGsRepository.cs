namespace Domain.GettingSubscribes
{
    public interface ITaskGsRepository
    {
        void Create(TaskGS taskGS);
        void Update(TaskGS taskGS);
        TaskGS GetBy(long taskId, bool taskDeleted = false);
        TaskGS GetBy(string userToken, long taskId, bool taskDeleted = false);
    }
}
