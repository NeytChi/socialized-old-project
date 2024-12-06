using Newtonsoft.Json.Linq;
using Domain.GettingSubscribes;
using Serilog;

namespace UseCases.Tasks
{
    public class BaseHandler : IHandlerGS
    {
        public ILogger Logger;
        public IHandlerGS Handler;

        public BaseHandler(ILogger logger)
        {
            Logger = logger;
        }
        public void setNext(IHandlerGS handler)
        {
            Handler = handler;
        }
        public bool handle(JObject json, TaskGS task, ref string message)
        {
            return true;
        }
    }
}