using System.Linq;
using Serilog.Core;
using database.context;
using InstagramService;
using Models.GettingSubscribes;
using System.Collections.Generic;

namespace Controllers
{
    public class TaskDataManager
    {
        public Logger log;
        public Context context;
        public TaskDataManager(Logger log, Context context)
        {
            this.context = context;
            this.log = log;
        }
        public TaskData GetNonDeleteData(long dataId, string userToken, ref string message)
        {
            TaskData data = (from d in context.TaskData
            join t in context.TaskGS on d.taskId equals t.taskId
            join s in context.IGAccounts on t.sessionId equals s.accountId
            join u in context.Users on s.userId equals u.userId
            where u.userToken == userToken
                && d.dataId == dataId 
                && d.dataDeleted == false select d).FirstOrDefault();
            if (data == null)
                message = "Server can't define data."; 
            return data;
        }
        public List<TaskData> GetNonDeleteDataList(long taskId)
        {
            List<TaskData> data = context.TaskData.Where(d 
                => d.dataDeleted == false 
                && d.taskId == taskId).ToList();
            if (data == null)
                log.Warning("Server can't define data."); 
            return data;
        }
        public TaskGS GetNonDeleteTask(long taskId, ref string message)
        {
            TaskGS task = (from t in context.TaskGS
            where t.taskId == taskId 
                && t.taskDeleted == false 
                select t).FirstOrDefault();
            if (task == null)
                message = "Server can't define task."; 
            return task;
        }
        public TaskGS GetNonDeleteTask(long taskId, string userToken, ref string message)
        {
            TaskGS task = (from t in context.TaskGS
            join s in context.IGAccounts on t.sessionId equals s.accountId
            join u in context.Users on s.userId equals u.userId
            where u.userToken == userToken
                && t.taskId == taskId 
                && t.taskDeleted == false select t).FirstOrDefault();
            if (task == null)
                message = "Server can't define task."; 
            return task;
        }
        public void DeleteData(TaskData data, ref string message)
        {
            data.dataDeleted = true;
            data.dataStopped = true;
            context.TaskData.Update(data);
            context.SaveChanges();
            DeleteStopTask(data.taskId);
            message = "Data was deleted.";
            log.Information(message);
        }
        public void DeleteStopTask(long taskId)
        {
            string message = null;
            TaskGS task = GetNonDeleteTask(taskId, ref message);
            if (task != null)
            {
                task.taskData = GetNonDeleteDataList(task.taskId);
                StopTask(ref task, task.taskData);
                DeleteTask(ref task, task.taskData.Count);   
                task.taskUpdated = true;
                context.TaskGS.Update(task);
                context.SaveChanges();
            }
            else
                log.Warning("The current data is not relevant because it's task has been deleted.");
        }
        public void StopTask(ref TaskGS task, ICollection<TaskData> taskData)
        {
            if (taskData.All(d => d.dataStopped))
            {
                task.taskStopped = true;
                log.Information("All task's data was stopped, id ->" + task.taskId);
            }
        }
        public void DeleteTask(ref TaskGS task, long dataCount)
        {
            if (dataCount == 0) {
                task.taskStopped = true;
                task.taskDeleted = true;
                log.Information("All data was stopped and task was deleted, id ->" + task.taskId);
            }
        }
        public void StartStopData(TaskData data, ref string message)
        {
            bool stopped = data.dataStopped;
            if (stopped) {
                data.dataStopped = false;
                message = "Data was started."; 
            }
            else {
                data.dataStopped = true;
                message = "Data was stopped.";
            }
            log.Information(message);
            context.TaskData.Update(data);
            context.SaveChanges();
            StartStopTask(data.taskId, stopped);
        }
        public void StartStopTask(long taskId, bool dataStopped)
        {
            string message = null;
            TaskGS task = GetNonDeleteTask(taskId, ref message);
            if (task != null) {
                if (dataStopped) {
                    task.taskStopped = false;
                    log.Information("Task was started.");
                }
                else {
                    task.taskData = GetNonDeleteDataList(task.taskId);
                    StopTask(ref task, task.taskData);
                    log.Information("Task was stopped.");
                }
                context.TaskGS.Update(task);
                context.SaveChanges();
            }
            else
                log.Warning("The current data is not relevant because it's task has been deleted.");
        }
        public TaskData GetTaskData(TaskSubtype subtype, TaskCache taskCache, ref string message)
        {
            switch(subtype)
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
                default : message = "Unknow number of taskSubtype. Can't define subtype of task."; 
                    return null;
            }
        }
        public TaskData GetLocation(TaskCache taskCache, ref string message)
        {
            if (!string.IsNullOrEmpty(taskCache.location_name))
            {
                TaskData taskData = new TaskData();
                taskData.dataNames = taskCache.location_name;
                taskData.dataLongitute = taskCache.longitude;
                taskData.dataLatitute = taskCache.latitude;
                return taskData;
            }
            else
                message = "Hashtag is null or empty."; 
            return null;
        }
        public TaskData GetHashtag(TaskCache taskCache, ref string message)
        {
            if (!string.IsNullOrEmpty(taskCache.hashtag))
            {
                TaskData taskData = new TaskData();
                taskData.dataNames = taskCache.hashtag;
                return taskData;
            }
            else
                message = "Hashtag is null or empty."; 
            return null;
        }
        public TaskData GetUsername(TaskCache taskCache, ref string message)
        {
            if (!string.IsNullOrEmpty(taskCache.username))
            {
                TaskData taskData = new TaskData();
                taskData.dataNames = taskCache.username;
                return taskData;
            }
            else
                message = "Username is null or empty."; 
            return null;
        }
        public void SaveTaskData(TaskGS task, TaskData taskData)
        {
            taskData.taskId = task.taskId;
            context.TaskData.Add(taskData);
            context.SaveChanges();
            task.taskUpdated = true;
            task.taskStopped = false;
            context.TaskGS.Update(task);
            context.SaveChanges();
            log.Information("Create new task's data, id ->" + taskData.dataId);
        }
        public dynamic GetTaskDataOutput(TaskData taskData)
        {
            if (taskData != null)
                return new 
                {
                    data_id = taskData.dataId,
                    data_names = taskData.dataNames,
                    data_longitute = taskData.dataLongitute,
                    data_latitude = taskData.dataLatitute,
                    data_running = taskData.dataStopped == false ? true : false,
                };
            return null;
        }
    }
}