using Newtonsoft.Json.Linq;
using Domain.GettingSubscribes;

namespace UseCases.Tasks
{
    public interface IHandlerGS
    {
        void setNext(IHandlerGS handler);
        bool handle(JObject json, TaskGS task, ref string message);
    }
}