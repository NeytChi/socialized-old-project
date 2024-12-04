using Serilog;
using Domain.GettingSubscribes;

namespace UseCases.AutoPosts
{
    public class TaskDataManager
    {
        private ILogger Logger;
        private ITaskDataRepository TaskDataRepository;
        private ITaskGsRepository TaskGsRepository;

        public TaskDataManager(ILogger logger,
            ITaskDataRepository taskDataRepository,
            ITaskGsRepository taskGsRepository)
        {
            Logger = logger;
            TaskDataRepository = taskDataRepository;
            TaskGsRepository = taskGsRepository;
        }
        public TaskData GetNonDeleteData(long dataId, string userToken, ref string message)
        {
            var data = TaskDataRepository.GetBy(userToken, dataId);
            if (data == null)
                message = "Server can't define data.";
            return data;
        }
        public TaskGS GetNonDeleteTask(long taskId, string userToken, ref string message)
        {
            var task = TaskGsRepository.GetBy(userToken, taskId);
            if (task == null)
                message = "Server can't define task.";
            return task;
        }
        public void DeleteData(TaskData data, ref string message)
        {
            data.dataDeleted = true;
            data.dataStopped = true;
            TaskDataRepository.Update(data);
            DeleteStopTask(data.taskId);
            message = "Data was deleted.";
            Logger.Information(message);
        }
        public void DeleteStopTask(long taskId)
        {
            var task = TaskGsRepository.GetBy(taskId);
            if (task != null)
            {
                task.taskData = TaskDataRepository.GetBy(taskId);
                if (task.taskData != null)
                {
                    StopTask(ref task, task.taskData);
                    DeleteTask(ref task, task.taskData.Count);
                    task.taskUpdated = true;
                    TaskGsRepository.Update(task);
                }
                else
                {
                    Logger.Warning("Server can't define data.");
                }
            }
            else
            {
                Logger.Warning("The current data is not relevant because it's task has been deleted.");
            }
        }
        public void StopTask(ref TaskGS task, ICollection<TaskData> taskData)
        {
            if (taskData.All(d => d.dataStopped))
            {
                task.taskStopped = true;
                Logger.Information("All task's data was stopped, id ->" + task.taskId);
            }
        }
        public void DeleteTask(ref TaskGS task, long dataCount)
        {
            if (dataCount == 0)
            {
                task.taskStopped = true;
                task.taskDeleted = true;
                Logger.Information("All data was stopped and task was deleted, id ->" + task.taskId);
            }
        }
        public void StartStopData(TaskData data, ref string message)
        {
            bool stopped = data.dataStopped;
            if (stopped)
            {
                data.dataStopped = false;
                message = "Data was started.";
            }
            else
            {
                data.dataStopped = true;
                message = "Data was stopped.";
            }
            Logger.Information(message);
            TaskDataRepository.Update(data);
            StartStopTask(data.taskId, stopped);
        }
        public void StartStopTask(long taskId, bool dataStopped)
        {
            var task = TaskGsRepository.GetBy(taskId);
            if (task != null)
            {
                if (dataStopped)
                {
                    task.taskStopped = false;
                    Logger.Information("Task was started.");
                }
                else
                {
                    task.taskData = TaskDataRepository.GetBy(taskId);
                    StopTask(ref task, task.taskData);
                    Logger.Information("Task was stopped.");
                }
                TaskGsRepository.Update(task);
            }
            else
            {
                Logger.Warning("The current data is not relevant because it's task has been deleted.");
            }
        }
        public TaskData GetTaskData(TaskSubtype subtype, TaskCache taskCache, ref string message)
        {
            switch (subtype)
            {
                case TaskSubtype.Like:
                case TaskSubtype.ByLikers:
                case TaskSubtype.ByCommentators:
                case TaskSubtype.ByUserFollowers:
                case TaskSubtype.ByList:
                case TaskSubtype.Comment:
                    return GetUsername(taskCache, ref message);
                case TaskSubtype.ByHashtag:
                    return GetHashtag(taskCache, ref message);
                case TaskSubtype.ByLocation:
                    return GetLocation(taskCache, ref message);
                case TaskSubtype.FromYourFollowing:
                    message = "This subtype of task doesn't have access to add data.";
                    return null;
                default:
                    message = "Unknow number of taskSubtype. Can't define subtype of task.";
                    return null;
            }
        }
        public TaskData GetLocation(TaskCache taskCache, ref string message)
        {
            if (!string.IsNullOrEmpty(taskCache.location_name))
            {
                var taskData = new TaskData
                {
                    dataNames = taskCache.location_name,
                    dataLongitute = taskCache.longitude,
                    dataLatitute = taskCache.latitude
                };
                return taskData;
            }
            else
            {
                message = "Hashtag is null or empty.";
            }
            return null;
        }
        public TaskData GetHashtag(TaskCache taskCache, ref string message)
        {
            if (!string.IsNullOrEmpty(taskCache.hashtag))
            {
                var taskData = new TaskData();
                taskData.dataNames = taskCache.hashtag;
                return taskData;
            }
            else
            {
                message = "Hashtag is null or empty.";
            }
            return null;
        }
        public TaskData GetUsername(TaskCache taskCache, ref string message)
        {
            if (!string.IsNullOrEmpty(taskCache.username))
            {
                var taskData = new TaskData();
                taskData.dataNames = taskCache.username;
                return taskData;
            }
            else
            {
                message = "Username is null or empty.";
            }
            return null;
        }
        public void SaveTaskData(TaskGS task, TaskData taskData)
        {
            taskData.taskId = task.taskId;
            TaskDataRepository.Update(taskData);
            task.taskUpdated = true;
            task.taskStopped = false;
            TaskGsRepository.Update(task);
            Logger.Information("Create new task's data, id ->" + taskData.dataId);
        }
        public dynamic GetTaskDataOutput(TaskData taskData)
        {
            if (taskData != null)
            {
                return new
                {
                    data_id = taskData.dataId,
                    data_names = taskData.dataNames,
                    data_longitute = taskData.dataLongitute,
                    data_latitude = taskData.dataLatitute,
                    data_running = taskData.dataStopped == false ? true : false,
                };
            }
            return null;
        }
    }
}