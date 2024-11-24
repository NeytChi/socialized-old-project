using System.Linq;
using Serilog.Core;
using database.context;
using Newtonsoft.Json.Linq;
using Domain.GettingSubscribes;

namespace UseCases.Tasks
{
    public class TaskCheck : BaseHandler, IHandlerGS
    {
        public Context context;
        public TaskCheck(Logger log, Context context):base(log)
        {
            this.context = context;
        }
        public new bool handle(JObject json, TaskGS task, ref string message)
        {
            TaskGS taskGS = GetNonDeleteTask(task.taskType, task.taskSubtype, task.sessionId);
            if (taskGS == null) 
            {
                if (handler != null)
                {
                    return handler.handle(ref json, ref task, ref message);
                }
                return true;
            }
            else  
            {
                message = "User has already task with the same task type and task subtype.";
                Logger.Warning(message);
                return false;
            }
        }
        public TaskGS GetNonDeleteTask(sbyte taskType, sbyte taskSubtype, long sessionId)
        {
            Logger.Information("Get non-delete task, session id -> " + sessionId);
            return context.TaskGS.Where(t 
                => t.taskType == taskType 
                && t.taskSubtype == taskSubtype 
                && t.sessionId == sessionId 
                && t.taskDeleted == false).FirstOrDefault();
        }
    }
}