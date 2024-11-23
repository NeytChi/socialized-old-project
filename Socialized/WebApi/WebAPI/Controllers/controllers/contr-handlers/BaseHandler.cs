using Newtonsoft.Json.Linq;
using Models.GettingSubscribes;

namespace Controllers
{
    public class BaseHandler : IHandlerGS
    {
        public IHandlerGS handler;
        public void setNext(IHandlerGS handler)
        {
            this.handler = handler;
        }
        public bool handle(ref JObject json, ref TaskGS task, ref string message)
        {
            return true;
        }
    }
}