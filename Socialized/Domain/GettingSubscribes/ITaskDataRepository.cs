namespace Domain.GettingSubscribes
{
    public interface ITaskDataRepository
    {
        void Create(TaskData taskData);
        void Update(TaskData taskData);
        TaskData GetBy(string userToken, long dataId, bool deleted = false);
        List<TaskData> GetBy(long taskId, bool deleted = false);
    }
}
