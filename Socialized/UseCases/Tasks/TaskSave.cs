using Serilog;
using Newtonsoft.Json.Linq;
using Domain.GettingSubscribes;

namespace UseCases.Tasks
{
    public class TaskSave : BaseHandler, IHandlerGS
    {
        private Context context;
        public TaskSave(ILogger logger, Context context):base(logger)
        {
            this.context = context;
        }
        public new bool handle(ref JObject json,ref TaskGS task, ref string message)
        {
            context.TaskGS.Add(task);
            context.SaveChanges();
            logger.Information("Save new task, id -> " + task.taskId);
            if (handler != null)
            {
                return handler.handle(ref json, ref task, ref message);
            }
            else
            {
                return true;
            }
        }
    }
}