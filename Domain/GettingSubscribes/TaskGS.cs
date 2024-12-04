using Domain.Users;
using Domain.SessionComponents;

namespace Domain.GettingSubscribes
{
    ///<summary>
    /// Task of "Getting Subscribers"
    ///<summary>
    public partial class TaskGS
    {
        public TaskGS()
        {
            taskData = new HashSet<TaskData>();
        }
        public long taskId { get; set; }
        public long sessionId { get; set; }
        public sbyte taskType { get; set; }
        public sbyte taskSubtype { get; set; }
        public long createdAt { get; set; }
        public long lastDoneAt { get; set; }
        public bool taskUpdated { get; set; }
        public bool taskRunning { get; set; }
        public bool taskStopped { get; set; }
        public long nextTaskData { get; set; }
        public bool taskDeleted { get; set; }
        public virtual IGAccount account { get; set; }
        public virtual User User { get; set; }
        public virtual TaskFilter taskFilter { get; set; }
        public virtual TaskOption taskOption { get; set; }
        public virtual ICollection<TaskData> taskData { get; set; }
        public virtual ICollection<HistoryGS> Histories { get; set; }
    }
    public struct TaskCache
    {
        public string user_token { get; set; }
        public long session_id { get; set; }
        public long task_id { get; set; }
        public long data_id { get; set; }
        public string username { get; set; }
        public string hashtag { get; set; }
        public string location_name { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
    }
}