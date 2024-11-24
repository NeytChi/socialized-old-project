using Serilog;
using InstagramService;
using InstagramApiSharp.API;
using InstagramApiSharp.Enums;
using InstagramApiSharp.Classes;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using Domain.AutoPosting;

namespace AutoPosting
{
    public class AutoDeleting
    {
        private ILogger Logger;
        private IAutoPostRepository AutoPostRepository;
        private IPostFileRepository PostFileRepository;
        public SessionManager smanager;
        public SessionStateHandler stateHandler;
        public InstagramApi api;
        
        public AutoDeleting(ILogger logger,
            IAutoPostRepository autoPostRepository,
            IPostFileRepository postFileRepository)
        {
            Logger = logger;
            AutoPostRepository = autoPostRepository;
            PostFileRepository = postFileRepository;
            this.smanager = new SessionManager(contextPosting);
            this.stateHandler = new SessionStateHandler(contextPosting);
            this.api = InstagramApi.GetInstance();
        }
        public bool RouteAutoDelete(AutoPost post, Session session)
        {
            if (post.postType)
            {
                return PerformDeletePost(post, ref session);
            }
            return PerformDeleteStories(post, ref session);
        }
        public void PerformAutoDelete(AutoPost post)
        {
            if (post.deleteAfter > DateTime.Now) 
            {
                Logger.Information("Auto delete was stoped on " + (post.executeAt - DateTime.Now).TotalSeconds + " seconds");
                Thread.Sleep(post.deleteAfter - DateTime.Now);
            }
            var session = smanager.LoadSession(post.sessionId);
            if (session != null) 
            {
                RouteAutoDelete(post, session);
                return;
            }
            Logger.Warning("Session wasn't load(id -> " + post.sessionId + "). Can't perform auto-delete post, id -> "
                + post.postId);
        }
        
        public bool PerformDeletePost(AutoPost post, ref Session session)
        {
            var deletePost = PostFileRepository.GetBy(post.postId);
            if (DeletePost(deletePost, ref session)) 
            {
                post.postAutoDeleted = true;
                AutoPostRepository.Update(post);
                Logger.Information("Set auto post deleted, id -> " + post.postId);
                return true;
            }
            return false;
        }
        public bool PerformDeleteStories(AutoPost post, ref Session session)
        {
            post.files = PostFileRepository.GetBy(post.postId, false);
            foreach (var file in post.files) 
            {
                if (!DeleteStory(ref session, file))
                {
                    return false;
                }
            }
            post.postAutoDeleted = true;
            AutoPostRepository.Update(post);
            Logger.Information("Set auto post deleted, id -> " + post.postId);
            return true;
        }
        
        public bool DeletePost(PostFile post, ref Session session)
        {
            var result = api.media.DeleteMedia(ref session, post.mediaId, InstaMediaType.Carousel);
            if (result.Succeeded)
            {
                Logger.Information("Create auto delete for post, user id -> " + session.userId);
                return true;
            }
            else
            {
                stateHandler.HandleState(result.Info.ResponseType, session);
            }
            Logger.Error("Can't create auto delete for post, user id -> " + session.userId);
            return false;
        }
        public bool DeleteStory(ref Session session, PostFile file)
        {
            var type = file.fileType ? InstaSharingType.Video : InstaSharingType.Photo;
            var result = api.story.DeleteStory(ref session, file.mediaId, type);
            if (result.Succeeded)
            {
                Logger.Information("Create auto delete for story, user id -> " + session.userId);
                return true;
            }
            else
            {
                stateHandler.HandleState(result.Info.ResponseType, session);
            }
            Logger.Error("Can't create auto delete for story, user id -> " + session.userId);
            return false;
        }
    }
}