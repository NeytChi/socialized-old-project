using Serilog.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace UseCases.Tasks
{
    public class TaskDefine : BaseHandler, IHandlerGS
    {
        private ILogger Logger;

        public TaskDefine(Logger logger) : base (logger)
        {
            Logger = logger;
        }
        public new bool handle(JObject json, TaskGS task, ref string message)
        {
            if ((task.taskType = DefineSbyte(ref json, "task_type", ref message)) == -1)
            {
                Logger.Warning(message);
                return false;
            }
            if ((task.taskSubtype = DefineSbyte(ref json, "task_subtype", ref message)) != -1)
            {
                Logger.Information("Define task's type & subtype");
                if (handler != null)
                {
                    return handler.handle(ref json, ref task, ref message);
                }
                return true;
            }

            Logger.Warning(message);
            return false;
        }
        public sbyte DefineSbyte(JObject json, string valueName, ref string message)
        {
            var taskType = handle(ref json, valueName, JTokenType.Integer, ref message);
            if (taskType != null) 
            {
                if (json.GetValue(valueName).ToObject<long>() > 0 && json.GetValue(valueName).ToObject<long>() < 127)
                {
                    return json.GetValue(valueName).ToObject<sbyte>();
                }
                else
                {
                    message = "Value '" + valueName + "' is bigger that 127 || less 0";
                }
            }
            return -1;
        }
    }
}