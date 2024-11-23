using Serilog.Core;
using database.context;
using Newtonsoft.Json.Linq;
using Models.GettingSubscribes;

namespace Controllers
{
    public class TaskSave : JsonHandler, IHandlerGS
    {
        private Context context;
        public TaskSave(Logger log, Context context):base(log)
        {
            this.log = log;
            this.context = context;
        }
        public new bool handle(ref JObject json,ref TaskGS task, ref string message)
        {
            context.TaskGS.Add(task);
            context.SaveChanges();
            log.Information("Save new task, id -> " + task.taskId);
            if (handler != null)
                return handler.handle(ref json, ref task, ref message);
            else
                return true;
        }
    }
}