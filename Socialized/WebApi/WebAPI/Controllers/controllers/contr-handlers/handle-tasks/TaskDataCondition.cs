using Managment;
using Serilog.Core;
using InstagramService;
using Newtonsoft.Json.Linq;
using InstagramApiSharp.API;
using Models.GettingSubscribes;
using InstagramApiSharp.Classes;
using System.Collections.Generic;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;

namespace Controllers
{
    public class TaskDataCondition : JsonHandler, IHandlerGS
    {
        public TaskDataCondition(Logger log, SessionManager sessionManager, SessionStateHandler stateHandler):base(log)
        {
            this.log = log;
            this.sessionManager = sessionManager;
            this.api = InstagramApi.GetInstance();
            this.stateHandler = stateHandler;
        }
        InstagramApi api;
        SessionManager sessionManager;
        SessionStateHandler stateHandler;
        public new bool handle(ref JObject json, ref TaskGS task, ref string message)
        {
            bool success = false;
            switch((TaskSubtype)task.taskSubtype)
            {
                case TaskSubtype.ByLikers:
                case TaskSubtype.ByUserFollowers:
                case TaskSubtype.ByCommentators:
                case TaskSubtype.ByList:
                case TaskSubtype.Like:
                case TaskSubtype.Comment:
                    success = CheckUsernames(task.taskData, task.sessionId, ref message);
                    break;
                case TaskSubtype.ByLocation:
                    success = CheckLocations(task.taskData, task.sessionId, ref message);
                    break;
                case TaskSubtype.ByHashtag:
                    success = CheckHashtags(task.taskData, task.sessionId, ref message);
                    break;
                case TaskSubtype.FromYourFollowing:
                    break;
                default : 
                    message = "Incorrect subtype of task.";
                    break;
            }
            log.Information("Check task's data, result -> " + success);
            if (handler != null && success)
                return handler.handle(ref json, ref task, ref message);
            return success;
        }
        public bool handle(TaskSubtype subtype, long sessionId, TaskData data, ref string message)
        {
            bool success = true;
            Session session = sessionManager.LoadSession(sessionId);
            switch(subtype)
            {
                case TaskSubtype.ByLikers:
                case TaskSubtype.ByUserFollowers:
                case TaskSubtype.ByCommentators:
                case TaskSubtype.ByList:
                case TaskSubtype.Like:
                case TaskSubtype.Comment:
                    success = CheckExistUser(ref session, data.dataNames, ref message);
                    break;
                case TaskSubtype.ByLocation:
                    success = CheckExistLocation(ref session, data.dataNames,
                    data.dataLatitute ?? 0, data.dataLongitute ?? 0, ref message);
                    break;
                case TaskSubtype.ByHashtag:
                    success = CheckExistHashtag(ref session, data.dataNames, ref message);
                    break;
                default : 
                    message = "Incorrect subtype of task.";
                    return false;
            }
            log.Information("Check task data. result -> " + success);
            return success;
        }
        public bool CheckUsernames(ICollection<TaskData> taskData, long sessionId, ref string message)
        {
            Session session = sessionManager.LoadSession(sessionId);
            if (taskData.Count != 0) {
                foreach(TaskData data in taskData) {
                    if (!CheckExistUser(ref session, data.dataNames, ref message))
                        return false;
                }
                return true;
            }
            else 
               log.Warning("Input taskData count is equal to 0.");
            return false;
        }
        public bool CheckLocations(ICollection<TaskData> taskData, long sessionId, ref string message)
        {
            Session session = sessionManager.LoadSession(sessionId);
            if (taskData.Count != 0) {
                foreach(TaskData data in taskData) {
                    if (!CheckExistLocation(ref session, data.dataNames, 
                    data.dataLatitute ?? 0, data.dataLongitute ?? 0, ref message))
                        return false;
                }
                return true;
            }
            else
                log.Warning("Input taskData count is equal to 0.");                    
            return false;
        }
        public bool CheckHashtags(ICollection<TaskData> taskData, long sessionId, ref string message)
        {
            Session session = sessionManager.LoadSession(sessionId);
            if (taskData.Count != 0) {
                foreach(TaskData data in taskData) {
                    if (!CheckExistHashtag(ref session, data.dataNames, ref message))
                        return false;
                }
                return true;
            }
            else 
                log.Warning("Input taskData count is equal to 0.");
            return false;
        }
        public bool CheckExistUser(ref Session session, string username, ref string message)
        {
            if (!string.IsNullOrEmpty(username)) {
                var result = api.users.GetUser(ref session, username);
                if (result.Succeeded)
                    return true;
                else if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                else
                    message = "Can't search user -> '" + username + "'";
            }
            else message = "Username is null or empty.";
            return false;
        }
        public bool CheckExistLocation(ref Session session, string query, double latitute, double longitute, ref string message)
        {
            if (!string.IsNullOrEmpty(query)) {
                var result = SearchLocation(ref session, latitute, longitute, query);
                if(result != null)
                    if (result.Count > 0)
                        return true;
                    else
                        message = "Can't search location -> '" + query + "'.";
            }
            else
                message = "Location name is null or empty.";
            return false;
        }
        public bool CheckExistHashtag(ref Session session, string hashtag, ref string message)
        {
            if (!string.IsNullOrEmpty(hashtag)) {
                IResult<InstaHashtag> result = api.hashtag.GetHashtagInfo(hashtag, ref session);
                if(result.Succeeded)
                    return true;
                else
                    if (result.unexceptedResponse)
                        stateHandler.HandleState(result.Info.ResponseType, session);
                    else
                        message = "Can't search hashtag -> '" + hashtag + "'.";
            }
            else
                message = "Hashtag is null or empty.";
            return false;
        }
        public InstaLocationShortList SearchLocation(ref Session session, double latitute, double longitute, string location)
        {
            IResult<InstaLocationShortList> result = api.location.SearchLocation(ref session, latitute, longitute, location);
            if (result.Succeeded && result.Value.Count > 0) {
                if ( result.Value.Count > 0)
                    return result.Value;
            }
            else if (result.unexceptedResponse)
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Warning("Can't search locations, id -> " + session.sessionId);
            return null;
        }
    }
}