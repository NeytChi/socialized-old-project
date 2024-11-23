using Domain.GettingSubscribes;

namespace UseCases.AutoPosts
{
    public class TaskGSManager
    {
        private ITaskGsRepository TaskGsRepository;

        public TaskGSManager(ITaskGsRepository taskGsRepository)
        {
            TaskGsRepository = taskGsRepository;
        }
        public TaskGS SelectTask(long taskId, long userId, ref string message)
        {
            var task = GetNonDelete(taskId, userId);
            if (task != null)
            {
                task.taskOption = context.TaskOptions.Where(o => o.taskId == task.taskId).FirstOrDefault();
                task.taskData = GetNonDeleteTaskData(task.taskId);
                task.taskFilter = context.TaskFilters.Where(f => f.taskId == task.taskId).FirstOrDefault();
                if (task.taskFilter != null)
                {
                    task.taskFilter.words = context.FilterWords.Where(w => w.filterId == task.taskFilter.filterId).ToList();
                }
            }
            else
            {
                message = "Server can't define task.";
            }
            return task;
        }
        public TaskGS GetNonDelete(long taskId, long userId)
        {
            var task = (from t in context.TaskGS
                           join s in context.IGAccounts on t.sessionId equals s.accountId
                           join u in context.Users on s.userId equals u.userId
                           where u.userId == userId
                               && t.taskId == taskId
                               && t.taskDeleted == false
                           select t).FirstOrDefault();
            if (task != null)
            {
                task.taskData = context.TaskData.Where(t
                    => t.taskId == taskId
                    && t.dataDeleted == false).ToList();
            }
            return task;
        }
        public bool Delete(long taskId, int userId, ref string message)
        {
            var task = GetNonDelete(taskId, userId);
            if (task != null)
            {
                task.taskDeleted = true;
                UpdateTask(task);
                message = "Task was deleted.";
                return true;
            }
            else
                message = "Server can't define task.";
            return false;
        }
        public bool StartStopTask(long taskId, int userId, ref string message)
        {
            var task = GetNonDelete(taskId, userId);
            if (task == null)
            {
                message = "Server can't define task.";
                return false;
            }
            return task.taskStopped ? StartTask(task, ref message) : StopTask(task, ref message);
        }
        public List<TaskData> GetNonDeleteTaskData(long taskId)
        {
            return context.TaskData.Where(d
                => d.taskId == taskId
                && d.dataDeleted == false).ToList();
        }
        public bool StartTask(TaskGS task, ref string message)
        {
            if (task != null)
            {
                UpdateTaskDataStopped(task.taskData, true);
                task.taskStopped = false;
                context.TaskGS.Update(task);
                context.SaveChanges();
                message = "Current task was started.";
                return true;
            }
            else
            {
                message = "Task is null.";
            }
            return false;
        }
        public bool StopTask(TaskGS task, ref string message)
        {
            if (task != null)
            {
                UpdateTaskDataStopped(task.taskData, false);
                task.taskStopped = true;
                context.TaskGS.Update(task);
                context.SaveChanges();
                message = "Current task was stopped.";
                return true;
            }
            else
            {
                message = "Task is null.";
            }
            return false;
        }
        public void UpdateTaskDataStopped(ICollection<TaskData> taskData, bool stopped)
        {
            foreach (var data in taskData)
            {
                data.dataStopped = !stopped;
            }
            context.TaskData.UpdateRange(taskData);
            context.SaveChanges();
        }
        public dynamic SelectCurrentTask(TaskGS task)
        {
            if (task != null)
            {
                return new
                {
                    task_id = task.taskId,
                    task_type = task.taskType,
                    task_subtype = task.taskSubtype,
                    type_name = GetTaskTypeName(task.taskType),
                    subtype_name = GetTaskSubtypeName(task.taskSubtype),
                    created_at = task.createdAt,
                    task_running = task.taskStopped == false ? true : false,
                    task_data = GetTaskDataOutput(task.taskData),
                    task_options = GetTaskOptionsOutput((TaskType)task.taskType, task.taskOption),
                    task_filters = GetTaskFilterOutput(task.taskFilter)
                };
            }
            return null;
        }
        public dynamic GetTaskFilterOutput(TaskFilter filter)
        {
            if (filter != null)
            {
                filter.Task = null;
                if (filter.words != null)
                {
                    filter.words_in_description = filter.words
                        .Where(w => w.wordUse == true)
                        .Select(w => w.wordValue).ToList();
                    filter.no_words_in_description = filter.words
                        .Where(w => w.wordUse == false)
                        .Select(w => w.wordValue).ToList();
                    filter.words = null;
                }
                return filter;
            }
            return null;
        }
        public void UpdateTask(TaskGS task)
        {
            task.taskUpdated = true;
            context.TaskGS.Update(task);
            context.SaveChanges();
        }
        public List<dynamic> SelectTasks(int userId, long sessionId, ref string message)
        {
            var account = GetNonDeleteSession(userId, sessionId);
            if (account != null)
            {
                var tasks = GetTaskWithTaskData(sessionId);
                var types = GetTaskTypes(tasks);
                var data = SortTaskByType(tasks, types);
                return data;
            }
            else
            {
                message = "Server can't define ig account.";
            }
            return null;
        }
        public List<dynamic> SortTaskByType(List<TaskGS> tasks, List<sbyte> types)
        {
            var data = new List<dynamic>();
            if (types != null)
            {
                foreach (sbyte type in types)
                {
                    var sortedTasks = AddSortedTaskByType(tasks, type);
                    data.Add(new
                    {
                        task_type = type,
                        type_name = GetTaskTypeName(type),
                        tasks = sortedTasks
                    });
                }
            }
            return data;
        }
        public List<dynamic> AddSortedTaskByType(List<TaskGS> tasks, sbyte type)
        {
            var outputTasks = new List<dynamic>();
            if (tasks != null)
            {
                foreach (TaskGS task in tasks)
                {
                    if (task.taskType == type)
                    {
                        outputTasks.Add(new
                        {
                            task_subtype = task.taskSubtype,
                            subtype_name = GetTaskSubtypeName(task.taskSubtype),
                            task_id = task.taskId,
                            created_at = task.createdAt,
                            task_running = task.taskStopped == false ? true : false,
                            task_data = GetTaskDataOutput(task.taskData.ToList())
                        });
                    }
                }
            }
            return outputTasks;
        }
        public List<sbyte> GetTaskTypes(List<TaskGS> tasks)
        {
            var types = new List<sbyte>();
            if (tasks != null)
            {
                foreach (TaskGS task in tasks)
                {
                    if (!types.Contains(task.taskType))
                    {
                        types.Add(task.taskType);
                    }
                }
            }
            return types;
        }
        public List<TaskGS> GetTaskWithTaskData(long sessionId)
        {
            return (from task in context.TaskGS
                    join taskData in context.TaskData on task.taskId
                    equals taskData.taskId into taskDatas
                    where task.sessionId == sessionId
                        && task.taskDeleted == false
                    select new TaskGS()
                    {
                        taskId = task.taskId,
                        sessionId = task.sessionId,
                        taskType = task.taskType,
                        taskSubtype = task.taskSubtype,
                        createdAt = task.createdAt,
                        lastDoneAt = task.lastDoneAt,
                        taskUpdated = task.taskUpdated,
                        taskRunning = task.taskRunning,
                        taskStopped = task.taskStopped,
                        nextTaskData = task.nextTaskData,
                        taskDeleted = task.taskDeleted,
                        taskData = taskDatas.ToList(),
                    }).ToList();
        }
        public IGAccount GetNonDeleteSession(int userId, long accountId)
        {
            return context.IGAccounts.Where(s
                => s.accountId == accountId && s.userId == userId && s.accountDeleted == false).FirstOrDefault();
        }
        public dynamic GetTaskOptionsOutput(TaskType type, TaskOption option)
        {
            if (option != null)
            {
                switch (type)
                {
                    case TaskType.Following:
                        return new
                        {
                            like_users_post = option.likesOnUser,
                            watch_stories = option.watchStories,
                            dont_follow_on_private = option.dontFollowOnPrivate,
                            auto_unfollow = option.autoUnfollow
                        };
                    case TaskType.Liking:
                        return new
                        {
                            watch_stories = option.watchStories,
                            likes_on_user = option.likesOnUser
                        };
                    case TaskType.Comments:
                        return new
                        {
                            watch_stories = option.watchStories
                        };
                    case TaskType.Unfollowing:
                        return new
                        {
                            like_users_post = option.likeUsersPost,
                            unfollow_only_from_non_reciprocal = option.unfollowNonReciprocal
                        };
                    case TaskType.Blocking:
                        return new
                        {
                            next_unlocking = option.nextUnlocking
                        };
                    default:
                        return null;
                }
            }
            return null;
        }

        public string GetTaskTypeName(sbyte taskType)
        {
            switch (taskType)
            {
                case 1:
                    return "Подписываться";
                case 2:
                    return "Лайкать";
                case 3:
                    return "Коментарии";
                case 4:
                    return "Отписываться";
                case 5:
                    return "Блокировать";
                case 6:
                    return "Просматривать \"Stories\"";
                default:
                    return "";
            }
        }
        public List<dynamic> GetTaskDataOutput(ICollection<TaskData> taskData)
        {
            List<dynamic> outputData = new List<dynamic>();
            if (taskData != null)
            {
                foreach (var Data in taskData)
                {
                    outputData.Add(new
                    {
                        data_id = Data.dataId,
                        data_names = Data.dataNames,
                        data_longitute = Data.dataLongitute,
                        data_latitude = Data.dataLatitute,
                        data_running = Data.dataStopped == false ? true : false
                    });
                }
            }
            return outputData;
        }
        public string GetTaskSubtypeName(sbyte taskSubtype)
        {
            switch (taskSubtype)
            {
                case 1:
                    return "По комментаторам";
                case 2:
                    return "По лайкерам";
                case 3:
                    return "По подписчикам";
                case 4:
                    return "По хэштегу";
                case 5:
                    return "По локации";
                case 6:
                    return "Лайкать";
                case 7:
                    return "Комментировать";
                case 8:
                    return "От своих подписок";
                case 9:
                    return "По листу";
                default:
                    return "";
            }
        }
        public bool CheckUsableSession(long sessionId, ref string message)
        {
            bool stateUsable = (from st in context.States
                                join s in context.IGAccounts on st.accountId equals s.accountId
                                where s.accountId == sessionId
                                select st.stateUsable).FirstOrDefault();
            if (!stateUsable)
                message = "Server can't define session or it's not usable.";
            return stateUsable;
        }
    }
}