using Serilog.Core;
using InstagramService;
using Newtonsoft.Json.Linq;
using Models.GettingSubscribes;
using System.Collections.Generic;

namespace Controllers
{
    public class TaskCondition : JsonHandler, IHandlerGS
    {
        public TaskCondition(Logger log):base(log)
        {
            this.log = log;
        }
        public new bool handle(ref JObject json,ref TaskGS task, ref string message)
        {
            HashSet<TaskData> taskData = GetData((TaskSubtype)task.taskSubtype, 
                ref json, ref message);
            if (taskData != null)
                task.taskData = taskData;
            else
                log.Warning(message);
            if (task.taskData != null && handler != null)
                return handler.handle(ref json, ref task, ref message);
            return taskData == null ? false : true;
        }
        public JObject DefineTaskData(ref JObject json, ref string message)
        {
            if (json.ContainsKey("task_data"))
                return json["task_data"].ToObject<JObject>();
            else 
                message = "Json doesn't have required field 'task_data'."; 
            return null;
        }
        public HashSet<TaskData> GetData(TaskSubtype subtype, ref JObject json, ref string message)
        {
            JObject taskData = DefineTaskData(ref json, ref message);
            if (taskData != null)
                switch(subtype)
                {
                    case TaskSubtype.Like:
                    case TaskSubtype.ByLikers: 
                    case TaskSubtype.ByCommentators: 
                    case TaskSubtype.ByUserFollowers:
                    case TaskSubtype.ByList:
                        return GetNamedData(taskData, "usernames", ref message);
                    case TaskSubtype.ByHashtag:
                        return GetNamedData(taskData, "hashtags", ref message);
                    case TaskSubtype.ByLocation: 
                        return GetLocationData(taskData, ref message);
                    case TaskSubtype.Comment:
                        HashSet<TaskData> data = GetNamedData(taskData, "usernames", ref message);
                        if (data != null)
                        {
                            TaskData comment = GetCommentData(taskData, ref message);
                            if (comment != null)
                            {
                                data.Add(comment);
                                return data;
                            }
                        }
                        break;
                    case TaskSubtype.FromYourFollowing: 
                        return new HashSet<TaskData>();
                    default : message = "Can't define subtype of task."; 
                        break; 
                }
            return null;
        }
        public TaskData GetCommentData(JObject taskData, ref string message)
        {
            JToken comment = handle(ref taskData, "comment",  
                JTokenType.String, ref message);
            if (comment != null)
            {
                TaskData commentData = new TaskData();
                commentData.dataComment = comment.ToObject<string>();
                return commentData;
            }
            return null;
        }
        public HashSet<TaskData> GetNamedData(JObject json, string dataName, ref string message)
        {
            JToken dataToken = handle(ref json, dataName, JTokenType.Array, ref message);
            if (dataToken != null)
            {
                JArray data = dataToken.ToObject<JArray>();
                if (data.Count != 0)
                {
                    HashSet<TaskData> taskData = new HashSet<TaskData>();
                    foreach (JToken name in data)
                    {
                        if (name.Type == JTokenType.String)
                        {
                            TaskData task = new TaskData();
                            task.dataNames = name.ToObject<string>();
                            taskData.Add(task);
                        }
                        else
                        {
                            message = "Value ->'" + name.ToString() + "' has incorrect format.";
                            return null;
                        }
                    }
                    return taskData;
                }
                message = "Massive of '" + dataName + "' count is zero.";
            }
            return null;    
        }
        public HashSet<TaskData> GetLocationData(JObject json, ref string message)
        {
            JToken locationsToken = handle(ref json, "locations",  JTokenType.Array, ref message);
            if (locationsToken != null)
            {
                JToken longitudesToken = handle(ref json, "longitudes",  JTokenType.Array, ref message);
                if (longitudesToken != null)
                {
                    JToken latitudesToken = handle(ref json, "latitudes",  JTokenType.Array, ref message);
                    if (latitudesToken != null)
                    {
                        JArray locations = json.GetValue("locations").ToObject<JArray>();
                        JArray latitudes = json.GetValue("latitudes").ToObject<JArray>();
                        JArray longitudes = json.GetValue("longitudes").ToObject<JArray>();
                        if (locations.Count == latitudes.Count && latitudes.Count == longitudes.Count)
                        {
                            if (locations.Count != 0)
                                return GetLocations(locations, latitudes, longitudes, ref message);
                            message = "Massive of locations names -> count == 0.";
                        }
                        else
                            message = "Count of locations, latitudes and longitudes are not equal to each other.";      
                    }
                }
            }
            return null;
        }
        private HashSet<TaskData> GetLocations(JArray locations, JArray latitudes, JArray longitudes, ref string message)
        {
            HashSet<TaskData> taskData = new HashSet<TaskData>();
            for (int i = 0; i < locations.Count; i++)
            {
                if (locations[i].Type == JTokenType.String &&
                    latitudes[i].Type == JTokenType.Float && 
                    longitudes[i].Type == JTokenType.Float)
                {
                    TaskData task = new TaskData();
                    task.dataLatitute = latitudes[i].ToObject<double>();
                    task.dataLongitute = longitudes[i].ToObject<double>();
                    task.dataNames = locations[i].ToObject<string>();
                    taskData.Add(task);
                }
                else 
                {
                    message = "Values in massive has incorrect format. " + 
                    "Values ->'" + locations[i].ToString() 
                    + "' ->'" + latitudes[i].ToString() 
                    + "' ->'" + longitudes[i].ToString() + "'.";
                    return null;
                }
            }
            return taskData;       
        }
    }
}