using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Serilog;
using Serilog.Core;

using Managment;
using database.context;
using Models.AutoPosting;

using InstagramService;
using InstagramApiSharp.API;
using InstagramApiSharp.Enums;
using InstagramApiSharp.Classes;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;

namespace nautoposting
{
    public class AutoDeleting
    {
        Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public SessionManager smanager;
        public SessionStateHandler stateHandler;
        public InstagramApi api;
        public Context contextPosting;
        
        public AutoDeleting()
        {
            this.contextPosting = new Context(false);
            this.smanager = new SessionManager(contextPosting);
            this.stateHandler = new SessionStateHandler(contextPosting);
            this.api = InstagramApi.GetInstance();
        }
        public bool RouteAutoDelete(AutoPost post, Session session)
        {
            if (post.postType)
                return PerformDeletePost(post, ref session);
            else
                return PerformDeleteStories(post, ref session);
        }
        /// <summary>
        /// Perform auto-delete post at the appointed time.
        /// </summary>
        public void PerformAutoDelete(AutoPost post)
        {
            if (post.deleteAfter > DateTime.Now) {
                log.Information("Auto delete was stoped on " + (post.executeAt - DateTime.Now).TotalSeconds + " seconds");
                Thread.Sleep(post.deleteAfter - DateTime.Now);
            }
            Session session = smanager.LoadSession(post.sessionId);
            if (session != null) {
                RouteAutoDelete(post, session);
                return;
            }
            log.Warning("Session wasn't load(id -> " + post.sessionId + "). Can't perform auto-delete post, id -> "
                + post.postId);
        }
        
        public bool PerformDeletePost(AutoPost post, ref Session session)
        {
            PostFile deletePost = contextPosting.PostFiles.Where(p => p.postId == post.postId).FirstOrDefault();
            if (DeletePost(deletePost, ref session)) {
                EndAutoDelete(post);
                return true;
            }
            return false;
        }
        public bool PerformDeleteStories(AutoPost post, ref Session session)
        {
            post.files = GetPostFiles(post.postId);
            foreach (PostFile file in post.files) {
                if (!DeleteStory(ref session, file))
                    return false;
            }
            EndAutoDelete(post);
            return true;
        }
        
        public bool DeletePost(PostFile post, ref Session session)
        {
            IResult<bool> result = api.media.DeleteMedia(ref session, post.mediaId, 
                InstaMediaType.Carousel);
            if (result.Succeeded) {
                log.Information("Create auto delete for post, user id -> " + session.userId);
                return true;
            }
            else
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Error("Can't create auto delete for post, user id -> " + session.userId);
            return false;
        }
        public bool DeleteStory(ref Session session, PostFile file)
        {
            InstaSharingType type = file.fileType ? InstaSharingType.Video : InstaSharingType.Photo;
            IResult<bool> result = api.story.DeleteStory(ref session, file.mediaId, type);
            if (result.Succeeded) {
                log.Information("Create auto delete for story, user id -> " + session.userId);
                return true;
            }
            else
                stateHandler.HandleState(result.Info.ResponseType, session);
            log.Error("Can't create auto delete for story, user id -> " + session.userId);
            return false;
        }
        public void EndAutoDelete(AutoPost post)
        {
            post.postAutoDeleted = true;
            contextPosting.AutoPosts.Attach(post).Property(p => p.postAutoDeleted).IsModified = true;
            contextPosting.SaveChanges();
            log.Information("Set auto post deleted, id -> " + post.postId);
        }
        public ICollection<PostFile> GetPostFiles(long postId)
        {
            return contextPosting.PostFiles.Where(f 
                => f.postId == postId
                && f.fileDeleted == false)
            .OrderBy(f => f.fileOrder).ToList();
        }
    }
}