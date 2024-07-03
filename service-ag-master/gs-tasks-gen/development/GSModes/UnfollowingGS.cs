using Serilog.Core;
using database.context;
using Models.GettingSubscribes;
using InstagramService;

namespace ngettingsubscribers
{
    public class UnfollowingGS : BaseModeGS, IModeGS
    {
        public UnfollowingGS(OptionsGS options, Logger log, SessionStateHandler handler): base (options)
        {
            this.log = log;
            this.options = options;
            this.stateHandler = handler;
            typeMode = 4;
        }
        public new bool HandleTask(Context context, ref TaskBranch branch)
        {
            long userPk = branch.currentUnit.userPk;
            bool unfollow = branch.currentTask.taskOption.unfollowNonReciprocal;
            if (options.GetAccessUnfollowNonReciprocal(ref branch.session, unfollow, userPk))
            {   
                if (UnfollowUser(context, ref branch))
                {
                    CheckOptions(context, ref branch);
                    return true;
                }
            }
            return false;
        }
        public new bool CheckOptions(Context context, ref TaskBranch branch)
        {
            bool optionEnable = branch.currentTask.taskOption.likeUsersPost;
            if (options.LikeUsersPost(ref branch.session, optionEnable, branch.currentUnit.userPk))
            {
                if(optionEnable)
                {
                    UpdateLikeAction(context, branch.sessionId);
                }
                return true;
            }
            return false;
        }
        public bool UnfollowUser(Context context, ref TaskBranch branch)
        {
            if (options.Unfollow(ref branch.session, branch.currentUnit.userPk))
            {
                UpdateUnfollowAction(context, branch.sessionId);
                return true;
            }
            return false;
        }
    }
}