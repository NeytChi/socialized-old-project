using System;
using Serilog;
using System.Linq;
using Serilog.Core;
using database.context;
using System.Threading;
using InstagramApiSharp;
using InstagramApiSharp.API;
using Models.GettingSubscribes;
using InstagramApiSharp.Classes;
using System.Collections.Generic;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramService;

namespace ngettingsubscribers
{
    /// <summary>
    /// This class provide functional for receiving Instagram units by task 'Getting Subscribes' 
    /// type and subtype and save by the help of InstasoftContext.
    /// </summary>
    public class ReceiverUnitsGS
    {
        Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public Semaphore block = new Semaphore(1, 1);
        public Context context;
        private InstagramApi api = InstagramApi.GetInstance();
        public SessionStateHandler stateHandler;
        public ReceiverUnitsGS()
        {
            context = new Context(true);
            stateHandler = new SessionStateHandler(context);
        }
        public ReceiverUnitsGS(Context context)
        {
            this.context = context;
            stateHandler = new SessionStateHandler(context);
        }
        public static ReceiverUnitsGS GetInstance()
        {
            if (instance == null)
                instance = new ReceiverUnitsGS();
            return instance;
        }
        public bool SetInstagramUnits(ref Session session, TaskData data, TaskSubtype subtype)
        {
            switch(subtype)
            {
                case TaskSubtype.Comment:
                case TaskSubtype.ByLikers:
                case TaskSubtype.ByHashtag:
                case TaskSubtype.ByLocation:
                case TaskSubtype.ByCommentators:
                case TaskSubtype.ByUserFollowers:
                    return GetSaveUsers(ref session, data, subtype);
                case TaskSubtype.Like:
                    return GetSaveComments(data, session);
                case TaskSubtype.FromYourFollowing:
                    return GetSaveFollowing(ref session, data);
                case TaskSubtype.ByList:
                    return GetSaveUser(ref session, data);
                default:
                    log.Error("Stop set instgram units, can't define task subtype, id -> " + session.sessionId);
                    return false;
            }
        }
        public bool GetSaveUsers(ref Session session, TaskData data, TaskSubtype subtype)
        {
            List<InstaUserShort> users = null;
            switch(subtype) {
                case TaskSubtype.ByCommentators:
                    users = GetUsersByCommentators(ref session,  data.dataNames, data.nextPage);
                    break;
                case TaskSubtype.ByLikers:
                    users = GetUsersLikers(ref session, data.dataNames, data.nextPage);
                    break;
                case TaskSubtype.Comment:
                case TaskSubtype.ByUserFollowers:
                    users = GetFollowers(ref session, data.dataNames, data.nextPage);
                    break;
                case TaskSubtype.ByHashtag:
                    users = GetUsersByHashtag(ref session, data.dataNames, data.nextPage);
                    break;
                case TaskSubtype.ByLocation:
                    users = GetUsersByLocation(ref session, data);
                    break;
                default:
                    log.Error("Stop handle task without task subtype, id -> " + session.sessionId);
                    break;
            }
            foreach(InstaUserShort user in users)
                SaveUserUnit(user, data.dataId);
            return true;
        }
        public bool GetSaveUser(ref Session session, TaskData taskData)
        {
            IResult<InstaUser> user = api.users.GetUser(ref session, taskData.dataNames);
            if (user.Succeeded) {
                SaveUserUnit(user.Value, taskData.dataId);
                return true;
            }
            return false;
        }
        public bool GetSaveComments(TaskData data, Session session)
        {
            List<InstaComment> comments = GetCommentsByCommentators(ref session, data.dataNames, data.nextPage);
            if (comments != null) {
                foreach(InstaComment comment in comments)
                    SaveCommentUnit(comment, data.dataId);
                return true;
            }
            return false;
        }
        public bool GetSaveFollowing(ref Session session, TaskData data)
        {
            List<InstaUserShort> userShorts = GetFollowing(ref session, 
            session.User.UserName, data.nextPage);
            if (userShorts != null) {
                userShorts = RemoveExcessUsers(userShorts, data.taskId);
                foreach(InstaUserShort user in userShorts)
                    SaveUserUnit(user, data.dataId);
                return true;
            }
            return false;
        }
        public void SaveUserUnit(InstaUserShort user, long dataId)
        {
            UnitGS unit = new UnitGS();
            unit.dataId = dataId;
            unit.userPk = user.Pk;
            unit.userIsPrivate = user.IsPrivate;
            unit.username = user.UserName;
            unit.createdAt = (int)(DateTimeOffset.Now.ToUnixTimeSeconds());
            unit.unitHandled = false;
            block.WaitOne();
            context.Add(unit);
            context.SaveChanges();
            block.Release();
        }
        public void SaveCommentUnit(InstaComment comment, long dataId)
        {
            UnitGS unit = new UnitGS();
            unit.dataId = dataId;
            unit.userPk = comment.User.Pk;
            unit.userIsPrivate = comment.User.IsPrivate;
            unit.username = comment.User.UserName;
            unit.createdAt = (int)(DateTimeOffset.Now.ToUnixTimeSeconds());
            unit.unitHandled = false;
            unit.commentPk = comment.Pk.ToString();
            block.WaitOne();
            context.Add(unit);
            block.Release();
        }
        /// <summary>
        /// Get user's commentators from user's posts by Instagram username.
        /// </summary>
        public List<InstaUserShort> GetUsersByCommentators(ref Session session, string username, int nextPage)
        {
            InstaMediaList medias = GetUserMedia(ref session, username, nextPage);
            if (medias != null) {
                foreach(InstaMedia media in medias) {
                    List<InstaComment> comments = GetMediaComments(ref session, media.Pk, 0);
                    if (comments != null) {
                        log.Information("Get users by commentators, id -> " + session.sessionId);
                        return SortUsersFromComment(comments);
                    }
                }
            }
            return null;
        }
        public List<InstaUserShort> SortUsersFromComment(List<InstaComment> comments)
        {
            List<InstaUserShort> users = new List<InstaUserShort>();
            foreach (InstaComment comment in comments)
                users.Add(comment.User);
            return users;
        }
        public List<InstaComment> GetMediaComments(ref Session session, string mediaPk, int nextPage)
        {
            var pagination = new PaginationParameters();
            pagination.NextPage = nextPage;
            IResult<InstaCommentList> comments = api.comment.GetMediaComments(ref session,
            mediaPk, pagination);
            if (comments.Succeeded)
                return comments.Value.Comments;
            else {
                if (comments.unexceptedResponse)
                    stateHandler.HandleState(comments.Info.ResponseType, session);
                log.Warning("Can't get media's comments, id -> " + session.sessionId); 
            }
            return null;
        }
        public InstaMediaList GetUserMedia(ref Session session, string username, int nextPage)
        {
            var paginationMedia = new PaginationParameters();
            paginationMedia.NextPage = nextPage;
            var result = api.users.GetUserMedia(ref session, username, paginationMedia);
            if (result.Succeeded)
                return result.Value;
            else {
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                log.Warning("Can't get media list, id -> " + session.sessionId); 
            }   
            return null;
        }
        public List<InstaComment> GetCommentsByCommentators(ref Session session, string username, int nextPage)
        {
            var medias = GetUserMedia(ref session, username, nextPage);
            if (medias != null) {
                foreach (InstaMedia media in medias) {
                    var comments = GetMediaComments(ref session, media.Pk, 0);
                    if (comments != null) {
                        log.Information("Get comment by user's commentators, id -> " + session.sessionId);
                        return comments;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Get users-likers from user's posts by Instagram username.
        /// </summary>
        public List<InstaUserShort> GetUsersLikers(ref Session session, string username, int nextPage)
        {
            var medias = GetUserMedia(ref session, username, nextPage);
            if (medias != null) {
                foreach (InstaMedia media in medias) {
                    InstaLikersList likers = GetLikers(ref session, media.Pk);
                    if (likers != null) {
                        log.Information("Get user by users likers, id -> " + session.sessionId);
                        return likers;
                    }
                }
            }
            return null;
        }
        public InstaLikersList GetLikers(ref Session session, string mediaPk)
        {
            var result = api.media.GetMediaLikers(ref session, mediaPk);
            if (result.Succeeded)
                return result.Value;
            else  {
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                log.Warning("Can't get likers, id -> " + session.sessionId); 
            }   
            return null;
        }
        /// <summary>
        /// Get list of followers by Instagram location.
        /// </summary>
        public List<InstaUserShort> GetUsersByLocation(ref Session session, TaskData data)
        {
            InstaLocationShortList locations = SearchLocation(ref session, 
            data.dataLatitute ?? 0, data.dataLongitute ?? 0, data.dataNames);
            if (locations != null) {
                InstaLocationShort location = locations[0];
                InstaSectionMedia section = GetRecentLocationFeeds(ref session, location.ExternalId, data.nextPage);
                if (section != null) {
                    log.Information("Get users by location, id -> " + session.sessionId);
                    return SortUsersFromMedia(section);
                }
            }
            return null;
        }
        public List<InstaUserShort> SortUsersFromMedia(InstaSectionMedia section)
        {
            List<InstaUserShort> users = new List<InstaUserShort>();
            foreach (InstaMedia media in section.Medias)
                users.Add(media.User);
            return users;
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
        public InstaSectionMedia GetRecentLocationFeeds(ref Session session, string externalId, int nextPage)
        {
            PaginationParameters pagination = new PaginationParameters();
            pagination.NextPage = nextPage;
            var result = api.location.GetRecentLocationFeeds(ref session, Int64.Parse(externalId), pagination);
            if (result.Succeeded) {
                log.Information("Get recent location's feeds, id -> " + session.sessionId);
                return result.Value;    
            }
            else if (result.unexceptedResponse)
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Warning("Can't get location's feeds, id -> " + session.sessionId); 
            return null;
        }
        /// <summary>
        /// Get list of users by Instagram hashtag.
        /// </summary>
        public List<InstaUserShort> GetUsersByHashtag(ref Session session, string hashtag, int nextPage)
        {
            var pagination = new PaginationParameters();
            pagination.NextPage = nextPage;
            var sectionMedia = api.hashtag.GetRecentHashtagMediaList(hashtag, pagination, ref session);
            if (sectionMedia.Succeeded) {
                log.Information("Get users by hashtag, id -> " + session.sessionId);
                return SortUsersFromMedia(sectionMedia.Value);    
            }
            else if (sectionMedia.unexceptedResponse)
                stateHandler.HandleState(sectionMedia.Info.ResponseType, session);
            log.Warning("Can't get users by hashtag, id -> " + session.sessionId);
            return null;
        }
        /// <summary>
        /// Get user's following by Instagram username.
        /// </summary>
        public List<InstaUserShort> GetFollowing(ref Session session, string username, int nextPage)
        {
            PaginationParameters pagination = new PaginationParameters();
            pagination.NextPage = nextPage;
            var result = api.users.GetUserFollowing(ref session, username, pagination, string.Empty);
            if (result.Succeeded) {
                log.Information("Get user by user's following, id -> " + session.sessionId);
                return result.Value;
            }
            else if (result.unexceptedResponse)
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Information("Can't get following list by user, id -> " + session.sessionId); 
            return null;
        }
        /// <summary>
        /// Get user's followers by Instagram username.
        /// </summary>
        public List<InstaUserShort> GetFollowers(ref Session session, string username, int nextPage)
        {
            PaginationParameters pagination = new PaginationParameters();
            pagination.NextPage = nextPage;
            var result = api.users.GetUserFollowers(ref session, username, pagination, string.Empty);
            if (result.Succeeded) {
                log.Information("Get followers list by username, id ->" + session.sessionId);
                return result.Value;   
            }
            else if (result.unexceptedResponse)
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Information("Can't get followers list by username, id -> " + session.sessionId); 
            return null;
        }
        public List<InstaUserShort> RemoveExcessUsers(List<InstaUserShort> users, long taskId)
        {
            block.WaitOne();
            for (int i = users.Count - 1; i >= 0; i--) {
                if (context.TaskData.Any(d
                    => d.taskId == taskId
                    && d.dataNames == users[i].UserName 
                    && (d.dataStopped == false 
                    || d.dataDeleted == false)))
                    users.Remove(users[i]);
            }
            block.Release();
            log.Information("Sort from excess users.");
            return users;
        }
    }
}