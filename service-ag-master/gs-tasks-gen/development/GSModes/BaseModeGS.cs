using System;
using System.Linq;
using Serilog.Core;
using database.context;
using InstagramApiSharp.API;
using Models.GettingSubscribes;
using Models.SessionComponents;
using InstagramService;

namespace ngettingsubscribers
{
    public class BaseModeGS : IModeGS
    {
        public byte typeMode = 0;
        public InstagramApi api = InstagramApi.GetInstance();
        public OptionsGS options;
        public Logger log; 
        public SessionStateHandler stateHandler;
        public BaseModeGS(OptionsGS options)
        {
            this.options = options;
        }
        public bool CheckModeType(sbyte typeMode)
        {
            return this.typeMode == typeMode;
        }
        public bool HandleTask(Context context, ref TaskBranch branch)
        {
            return true;
        }
        public bool CheckOptions(Context context, ref TaskBranch branch)
        {
            return true;
        }
        public void UpdateWatchStories(Context context, long sessionId)
        {
            if (context != null)
            {
                TimesAction times = context.TimesAction.Where(t => t.sessionId == sessionId).First();
                ++times.watchingStoriesCount;
                times.watchingStoriesLastAt = DateTime.Now;
                context.TimesAction.Attach(times)
                .Property(a => a.watchingStoriesCount).IsModified = true;
                context.SaveChanges();
                context.TimesAction.Attach(times)
                .Property(a => a.watchingStoriesLastAt).IsModified = true;
                context.SaveChanges();
            }
        }
        public void UpdateLikeAction(Context context, long sessionId)
        {
            if (context != null)
            {
                TimesAction times = context.TimesAction.Where(t => t.sessionId == sessionId).First();
                ++times.likeCount;
                times.likeLastAt = DateTime.Now;            
                context.TimesAction.Attach(times)
                .Property(a => a.likeCount).IsModified = true;
                context.SaveChanges();
                context.TimesAction.Attach(times)
                .Property(a => a.likeLastAt).IsModified = true;
                context.SaveChanges();
            }
        }
        public void UpdateBlocking(Context context, long sessionId)
        {
            if (context != null)
            {
                TimesAction times = context.TimesAction.Where(t => t.sessionId == sessionId).First();
                ++times.blockCount;
                times.blockLastAt = DateTime.Now;
                context.TimesAction.Attach(times)
                .Property(a => a.blockCount).IsModified = true;
                context.SaveChanges();
                context.TimesAction.Attach(times)
                .Property(a => a.blockLastAt).IsModified = true;
                context.SaveChanges();
            }
        } 
        public bool OptionAutoUnfollow(Context context, ref TaskBranch branch)
        {
            if (context != null && branch != null)
            {
                if (options.AutoUnfollow(ref branch.session, branch.currentTask.taskOption.autoUnfollow, 
                branch.currentUnit.userPk))
                {
                    if (branch.currentTask.taskOption.autoUnfollow)
                    {
                        UpdateUnfollowAction(context, branch.sessionId);
                    }
                    return true;
                }
            }
            return false;
        }
        public void UpdateUnfollowAction(Context context, long sessionId)
        {
            if (context != null)
            {
                TimesAction times = context.TimesAction.Where(t => t.sessionId == sessionId).First();
                ++times.unfollowCount;
                times.unfollowLastAt = DateTime.Now;
                context.TimesAction.Attach(times)
                .Property(a => a.unfollowCount).IsModified = true;
                context.SaveChanges();
                context.TimesAction.Attach(times)
                .Property(a => a.unfollowLastAt).IsModified = true;
                context.SaveChanges();
            }
        }
        public void UpdateFollowAction(Context context, long sessionId)
        {
            if (context != null)
            {
                TimesAction times = context.TimesAction.Where(t => t.sessionId == sessionId).First();
                ++times.followCount;
                times.followLastAt = DateTime.Now;
                context.TimesAction.Attach(times)
                .Property(a => a.followCount).IsModified = true;
                context.SaveChanges();
                context.TimesAction.Attach(times)
                .Property(a => a.followLastAt).IsModified = true;
                context.SaveChanges();
            }
        }
        public void UpdateCommentAction(Context context, long sessionId)
        {
            if (context != null)
            {
                TimesAction times = context.TimesAction.Where(t => t.sessionId == sessionId).First();
                ++times.commentCount;
                times.commentLastAt = DateTime.Now;
                context.TimesAction.Attach(times)
                .Property(a => a.commentCount).IsModified = true;
                context.SaveChanges();
                context.TimesAction.Attach(times)
                .Property(a => a.commentLastAt).IsModified = true;
                context.SaveChanges();
            }
        }  
    }
}