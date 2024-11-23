using System.Linq;
using Serilog.Core;
using database.context;
using Newtonsoft.Json.Linq;
using Models.GettingSubscribes;

namespace Controllers
{
    public class TaskCheck : JsonHandler, IHandlerGS
    {
        public Context context;
        public TaskCheck(Logger log, Context context):base(log)
        {
            this.log = log;
            this.context = context;
        }
        public new bool handle(ref JObject json, ref TaskGS task, ref string message)
        {
            TaskGS taskGS = GetNonDeleteTask(task.taskType, task.taskSubtype, task.sessionId);
            if (taskGS == null) {
                if (handler != null)
                    return handler.handle(ref json, ref task, ref message);
                return true;
            }
            else  {
                message = "User has already task with the same task type and task subtype.";
                log.Warning(message);
                return false;
            }
        }
        public TaskGS GetNonDeleteTask(sbyte taskType, sbyte taskSubtype, long sessionId)
        {
            log.Information("Get non-delete task, session id -> " + sessionId);
            return context.TaskGS.Where(t 
                => t.taskType == taskType 
                && t.taskSubtype == taskSubtype 
                && t.sessionId == sessionId 
                && t.taskDeleted == false).FirstOrDefault();
        }
    }
}