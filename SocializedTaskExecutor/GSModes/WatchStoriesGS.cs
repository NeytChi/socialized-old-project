using Serilog.Core;
using database.context;
using Models.GettingSubscribes;
using InstagramService;

namespace ngettingsubscribers
{
    public class WatchStoriesGS : BaseModeGS, IModeGS
    {
        public WatchStoriesGS(OptionsGS options, Logger log, SessionStateHandler handler): base (options)
        {
            this.log = log;
            this.options = options;
            this.stateHandler = handler;
            typeMode = 6;
        }
        public new bool HandleTask(Context context,ref TaskBranch branch)
        {
            if (!branch.currentUnit.userIsPrivate)
            {
                if (WatchStoriesUsers(context, ref branch))
                    return true;
            }
            else
            {
                branch.currentUnit.unitHandled = false;
                log.Warning("Can't watch stories, because '" + branch.currentUnit.username + "' is private");
            }
            return false;
        }
        public bool WatchStoriesUsers(Context context, ref TaskBranch branch)
        {
            if (options.WatchStories(ref branch.session, true, branch.currentUnit.userPk))
            {
                UpdateWatchStories(context, branch.sessionId);
                return true;
            }
            return false;
        }
    }
} 