using System;
using Serilog;
using Serilog.Core;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using InstagramService;

namespace ngettingsubscribers
{
    public class OptionsGS
    {
        Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public SessionStateHandler stateHandler;
        private InstagramApi api;
        private OptionsGS(SessionStateHandler stateHandler)
        {
            this.stateHandler = stateHandler;
            api = InstagramApi.GetInstance();
        }
        public bool LikeUsersPost(ref Session session, bool optionEnable, long userPk)
        {
            if (optionEnable) {
                InstaMediaList medias = GetMedia(ref session, userPk, 0);
                if (medias != null) {
                    if (medias.Count >= 1) {
                        if (LikeMedia(ref session, medias[0].Pk)) {
                            log.Information("Like user's post by option, id ->" + session.sessionId);
                            return true; 
                        }
                    }
                    else {
                        log.Information("User doesn't have any media, id ->" + session.sessionId); 
                        return true; 
                    }
                }
            }
            else {
                log.Information("Like user's post option is switched off, id ->" + session.sessionId);
                return true;
            }
            return false;
        }
        public bool LikeMedia(ref Session session, string mediaPk)
        {
            IResult<bool> result = api.media.LikeMedia(ref session, mediaPk);
            if (result.Succeeded)
                return result.Value;
            else {
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                log.Warning("Can't like user's post, id -> " + session.sessionId);
            }
            return false;
        }
        public InstaMediaList GetMedia(ref Session session, long userPk, int nextPage)
        {
            PaginationParameters parameters = new PaginationParameters();
            parameters.NextPage = nextPage;
            IResult<InstaMediaList> result = api.users.GetUserMediaById(ref session, userPk, parameters);
            if (result.Succeeded)
                return result.Value; 
            else {
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                log.Warning("Can't get user's medias, id -> " + session.sessionId);
            }
            return null;
        }
        public bool WatchStories(ref Session session, bool optionEnable, long userPk)
        {
            if (optionEnable) {
                InstaReelFeed feed = GetUserStoryFeed(ref session, userPk);
                if (feed != null) {
                    if (MarkStoriesAsSeen(ref session, feed)) {
                        log.Information("Watch stories, id -> " + session.sessionId);
                        return true;
                    }
                }
                return false;
            }
            else 
                log.Information("Watch stories option is switched off, id -> " + session.sessionId);
            return true;
        }
        public bool MarkStoriesAsSeen(ref Session session, InstaReelFeed feed)
        {
            foreach(InstaStoryItem item in feed.Items) {
                IResult<bool> result = api.story.MarkStoryAsSeen(ref session, 
                item.Id, (int)(DateTimeOffset.Now.ToUnixTimeSeconds()));
                if (result.Succeeded) {
                    if (!result.Value) {
                        log.Warning("Can't mark all stories as seen. Mark is false, id -> " + session.sessionId);
                        return false;
                    }
                }
                else {
                    if (result.unexceptedResponse)
                        stateHandler.HandleState(result.Info.ResponseType, session);
                    log.Warning("Can't mark all stories as seen, id -> " + session.sessionId);
                    return false;
                }
            }
            return true;
        }
        public InstaReelFeed GetUserStoryFeed(ref Session session, long userPk)
        {
            IResult<InstaReelFeed> result = api.story.GetUserStoryFeed(ref session, userPk);
            if (result.Succeeded)
                return result.Value;
            else 
            { 
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                log.Warning("Can't get user story feed, id -> " + session.sessionId); 
            }
            return null;
        }
        public bool DontFollowOnPrivate(bool optionEnable, bool userIsPrivate)
        {
            if (optionEnable) {
                if (!userIsPrivate) {
                    log.Information("User can follow on a non-private account"); 
                    return true;
                }
                else 
                    log.Information("User has a private account"); 
            }
            else {
                log.Information("Option is switched off.");
                return true;
            }
            return false;
        }
        public bool AutoUnfollow(ref Session session, bool optionEnable, long userPk)
        {
            if (optionEnable) {
                if (Unfollow(ref session, userPk))
                    return true;
            }
            else {
                log.Information("Auto unfollow is switched off, id -> " + session.sessionId);
                return true;
            }
            return false;
        }
        public bool Unfollow(ref Session session, long userPk)
        {
            IResult<InstaFriendshipFullStatus> result = api.users.UnFollowUser(ref session, userPk);
            if (result.Succeeded)
                return true;
            else {
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                log.Warning("Can't unfollow, id -> " + session.sessionId);
            }
            return false;
        } 
        public bool GetAccessUnfollowNonReciprocal(ref Session session, bool optionEnable, long userPk)
        {
            if (optionEnable) {
                InstaStoryFriendshipStatus status = GetFriendshipStatus(ref session, userPk);
                if (status != null) {
                    if (!status.FollowedBy) {
                        log.Information("Get access to unfollow from non-reciprocal, id ->" + session.sessionId);
                        return true;
                    }
                    else 
                        log.Information("User followed by current user.");
                }
            }
            else {
                log.Information("Option is switched off.");
                return true; 
            }
            return false;
        }
        public InstaStoryFriendshipStatus GetFriendshipStatus(ref Session session, long userPk)
        {
            IResult<InstaStoryFriendshipStatus> result = api.users
            .GetFriendshipStatus(ref session, userPk);
            if (result.Succeeded)
                return result.Value;
            else { 
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, session);
                log.Warning("Can't get friendship status, id -> " + session.sessionId); 
            }
            return null;
        }
        public bool NextUnlocking(ref Session session, bool optionEnable, long userPk)
        {
            if (optionEnable) {
                IResult<InstaFriendshipFullStatus> result = api.users.UnBlockUser(ref session, userPk);
                if(result.Succeeded)
                    return true;
                else {
                    if (result.unexceptedResponse)
                        stateHandler.HandleState(result.Info.ResponseType, session);
                    log.Warning("Can't get unlock user, id -> " + session.sessionId); 
                }
            }
            else 
                return true; 
            return false;
        }
    }
}