using Serilog.Core;
using database.context;
using Models.GettingSubscribes;
using InstagramService;

namespace ngettingsubscribers
{
    public class FollowingGS : BaseModeGS, IModeGS
    {
        public FollowingGS(OptionsGS options, Logger log, SessionStateHandler handler): base (options)
        {
            this.log = log;
            this.options = options;
            this.stateHandler = handler;
            typeMode = 1;
        }
        public new bool HandleTask(Context context, ref TaskBranch branch)
        {
            if (context != null && branch != null)
            {
                bool optionPrivate = branch.currentTask.taskOption.dontFollowOnPrivate;
                if (options.DontFollowOnPrivate(optionPrivate, branch.currentUnit.userIsPrivate))
                {
                    if (FollowUser(context, ref branch))
                        return CheckOptions(context, ref branch);
                }
                else
                    branch.currentUnit.unitHandled = false;
            }
            return false;
        }
        public bool FollowUser(Context context, ref TaskBranch branch)
        {
            if (context != null && branch != null)
            {
                var result = api.users.FollowUser(ref branch.session, branch.currentUnit.userPk);
                if (result.Succeeded)
                {
                    UpdateFollowAction(context, branch.sessionId);
                    log.Information("Follow to user, id ->" + branch.currentTask.taskId);
                    return true;
                }
                else
                {
                    if (result.unexceptedResponse)
                        stateHandler.HandleState(result.Info.ResponseType, branch.session);
                    log.Warning("Can't follow to user; id ->" + branch.currentTask.taskId);
                }
            }
            return false;
        } 
       
        public new bool CheckOptions(Context context,ref TaskBranch branch)
        {
            if (!branch.currentUnit.userIsPrivate)
            {
                if (OptionLikesUser(context, ref branch))
                    if (OptionWatchStories(context, ref branch))
                        if (OptionAutoUnfollow(context, ref branch))
                            return true;
            }
            else
                return true;
            return false;
        }
        public bool OptionWatchStories(Context context, ref TaskBranch branch)
        {
            bool enableOption = branch.currentTask.taskOption.watchStories;
            if (options.WatchStories(ref branch.session, enableOption, branch.currentUnit.userPk))
            {
                if (enableOption)
                    UpdateWatchStories(context, branch.sessionId);
                return true;
            }
            return false;
        }
        public bool OptionLikesUser(Context context, ref TaskBranch branch)
        {
            bool enableOption = branch.currentTask.taskOption.likeUsersPost;
            if (options.LikeUsersPost(ref branch.session, enableOption, branch.currentUnit.userPk))
            {
                if (enableOption)
                    UpdateLikeAction(context, branch.sessionId);
                return true;
            }
            return false;
        }
    }
} 