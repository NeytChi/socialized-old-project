using Serilog.Core;
using database.context;
using Models.GettingSubscribes;
using InstagramService;

namespace ngettingsubscribers
{
    public class LikingGS : BaseModeGS, IModeGS
    {
        public ReceiverMediaGS mediaReceiver = ReceiverMediaGS.GetInstance();
        public LikingGS(OptionsGS options, Logger log, SessionStateHandler handler): base (options)
        {
            this.log = log;
            this.options = options;
            this.stateHandler = handler;
            typeMode = 2;
        }
        public new bool HandleTask(Context context,ref TaskBranch branch)
        {
            if (!branch.currentUnit.userIsPrivate)
                return CheckOptions(context, ref branch);
            else
            {
                branch.currentUnit.unitHandled = false;
                log.Information("Can't like '" + branch.currentUnit.username + "', because he is private.");
            }
            return false;
        }
        public new bool CheckOptions(Context context, ref TaskBranch branch)
        {
            if (OptionLikesUser(context, ref branch))
            {
                return OptionWatchStories(context, ref branch);
            }
            return false;
        }
        public bool OptionLikesUser(Context context, ref TaskBranch branch)
        {
            MediaGS media = mediaReceiver.GetMediaGS(context, branch.currentUnit, ref branch.session, 
            branch.currentTask.taskOption.likesOnUser);
            if (media != null)
            {
                if (options.LikeMedia(ref branch.session, media.mediaPk))
                {
                    branch.currentUnit.handleAgain = true;
                    branch.currentUnit.unitHandled = false;
                    UpdateLikeAction(context, branch.sessionId);
                    return true;
                }
            }
            else
            {
                branch.currentUnit.handleAgain = false;
                media = mediaReceiver.GetHandledMedia(context, branch.currentUnit.unitId);
                if (media != null)
                    branch.currentUnit.unitHandled = true;
                else
                    branch.currentUnit.unitHandled = false; 
            }
            return false;
        }
        public bool OptionWatchStories(Context context, ref TaskBranch branch)
        {
            if (options.WatchStories(ref branch.session, 
            branch.currentTask.taskOption.watchStories, branch.currentUnit.userPk))
            {
                if (branch.currentTask.taskOption.watchStories)
                {
                    UpdateWatchStories(context, branch.sessionId);
                }
                return true;
            }
            return false;
        }
    }
} 