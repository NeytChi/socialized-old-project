using Newtonsoft.Json.Linq;
using Models.GettingSubscribes;

namespace Controllers
{
    public interface IHandlerGS
    {
        void setNext(IHandlerGS handler);
        bool handle(ref JObject json, ref TaskGS task, ref string message);
    }
}