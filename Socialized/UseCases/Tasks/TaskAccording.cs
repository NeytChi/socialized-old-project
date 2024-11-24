using Serilog.Core;
using InstagramService;
using Newtonsoft.Json.Linq;
using Domain.GettingSubscribes;

namespace UseCases.Tasks
{
    /// <summary>
    /// This class check according task type to task subtype.
    /// </summary>
    public class TaskAccording : BaseHandler, IHandlerGS
    {
        public TaskAccording(Logger logger) : base(logger)
        {

        }
        public new bool handle(JObject json, TaskGS task, ref string message)
        {
            bool success = AccordingTask(
                (TaskType)task.taskType, (TaskSubtype)task.taskSubtype, ref message);
            if (success && handler != null)
            {
                return handler.handle(ref json, ref task, ref message);
            }
            return success;
        }
        public bool AccordingTask(TaskType taskType, TaskSubtype subtype, ref string message)
        {
            switch(taskType)
            {
                case TaskType.Liking:
                case TaskType.WatchStories:
                case TaskType.Following:
                    return BaseTasks(subtype, ref message);
                case TaskType.Blocking:
                case TaskType.Unfollowing:
                    return CutOffTasks(subtype, ref message);
                case TaskType.Comments:
                    return CommentTasks(subtype, ref message);
                case TaskType.Unknow:
                default:
                    message = "Server can't define task's type.";
                    return false;
            }
        }
        public bool BaseTasks(TaskSubtype subtype, ref string message)
        {
            if (subtype != TaskSubtype.Unknow) 
                if (subtype != TaskSubtype.Like)
                    if (subtype != TaskSubtype.Comment)
                        return true;
            message = "Task type isn't according to task subtype.";
            return false;
        }
        public bool CutOffTasks(TaskSubtype subtype, ref string message)
        {
            if (subtype == TaskSubtype.ByList)
                return true;
            else if (subtype == TaskSubtype.FromYourFollowing)
                return true;
            else
                message = "Task type isn't according to task subtype.";
            return false;
        }
        public bool CommentTasks(TaskSubtype subtype, ref string message)
        {
            if (subtype == TaskSubtype.Like)
                return true;
            else if (subtype == TaskSubtype.Comment)
                return true;
            else
                message = "Task type isn't according to task subtype.";                 
            return false;
        }
    }
}