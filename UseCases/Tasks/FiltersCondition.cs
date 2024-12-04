using Serilog.Core;
using Newtonsoft.Json.Linq;
using Domain.GettingSubscribes;

namespace UseCases.Tasks
{
    public class FiltersCondition : BaseHandler, IHandlerGS
    {
        public FiltersCondition(Logger log) : base(log)
        {
            this.log = log;
        }
        public new bool handle(JObject json, TaskGS task, ref string message)
        {
            task.taskFilter = CreateFilter(json);
            if (Handler != null)
                return handler.handle(json, task, ref message);
            return true;
        }
        public TaskFilter CreateFilter(JObject json)
        {
            string message = null;
            TaskFilter filter = null;
            JToken tokenFilters = handle(ref json, "task_filters", JTokenType.Object, ref message);
            if(tokenFilters != null)
            {
                filter = tokenFilters.ToObject<TaskFilter>();
                GetFilterWords(ref filter);
            }
            return filter;
        }
        public void GetFilterWords(ref TaskFilter filter)
        {   
            if (filter.words_in_description != null)
            {
                foreach(string word in filter.words_in_description)
                    filter.words.Add(new FilterWord(word, true));
            }
            if (filter.no_words_in_description != null)
            {
                foreach(string noWord in filter.no_words_in_description)
                    filter.words.Add(new FilterWord(noWord, false));
            }
        }
    }
}