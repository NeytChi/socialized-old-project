using Serilog.Core;
using database.context;
using Models.GettingSubscribes;
using InstagramService;

namespace ngettingsubscribers
{
    public class BlockingGS : BaseModeGS, IModeGS
    {
        public BlockingGS(OptionsGS options, Logger log, SessionStateHandler handler): base (options)
        {
            this.log = log;
            this.options = options;
            this.stateHandler = handler;
            typeMode = 5;
        }
        public new bool HandleTask(Context context, ref TaskBranch branch)
        {
            if (BlockingUser(context, ref branch))
            {
                CheckOptions(context, ref branch);
                return true;
            }
            return false;
        }
        public new bool CheckOptions(Context context, ref TaskBranch branch)
        {
            return options.NextUnlocking(ref branch.session, 
            branch.currentTask.taskOption.nextUnlocking, branch.currentUnit.userPk);
        }
        public bool BlockingUser(Context context, ref TaskBranch branch)
        {
            if (context != null && branch != null)
            {
                var result = api.users.BlockUser(ref branch.session, branch.currentUnit.userPk);
                if (result.Succeeded)
                {
                    UpdateBlocking(context, branch.sessionId);
                    log.Information("Block user by task, id -> "+ branch.currentTask.taskId);
                    return true;
                }
                else
                {
                    if (result.unexceptedResponse)
                        stateHandler.HandleState(result.Info.ResponseType, branch.session);
                    log.Information("Can't block user, id ->" + branch.currentTask.taskId);
                }
            }
            return false;
        }
    }
} 