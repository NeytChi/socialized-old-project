using Serilog.Core;
using Newtonsoft.Json.Linq;
using Models.GettingSubscribes;

namespace Controllers
{
    public class TaskDefine : JsonHandler, IHandlerGS
    {
        public TaskDefine(Logger log):base(log)
        {
            this.log = log;
        }
        public new bool handle(ref JObject json, ref TaskGS task, ref string message)
        {
            if ((task.taskType = DefineSbyte(ref json, "task_type", ref message)) != -1) {
                if ((task.taskSubtype = DefineSbyte(ref json, "task_subtype", ref message)) != -1) {
                    log.Information("Define task's type & subtype");
                    if (handler != null)
                        return handler.handle(ref json, ref task, ref message);
                    else
                        return true;
                }
            }
            log.Warning(message);
            return false;
        }
        public sbyte DefineSbyte(ref JObject json, string valueName, ref string message)
        {
            JToken taskType = handle(ref json, valueName, JTokenType.Integer, ref message);
            if (taskType != null) {
                if (json.GetValue(valueName).ToObject<long>() > 0 && json.GetValue(valueName).ToObject<long>() < 127)
                    return json.GetValue(valueName).ToObject<sbyte>();
                else
                    message = "Value '" + valueName + "' is bigger that 127 || less 0";
            }
            return -1;
        }
    }
}