using System.Linq;
using Serilog.Core;
using database.context;
using Models.GettingSubscribes;
using InstagramService;

namespace ngettingsubscribers
{
    public class CommentsGS : BaseModeGS, IModeGS
    { 
        public ReceiverMediaGS mediaReceiver = ReceiverMediaGS.GetInstance();
        public CommentsGS(OptionsGS options, Logger log, SessionStateHandler handler): base (options)
        {
            this.log = log;
            this.options = options;
            this.stateHandler = handler;
            typeMode = 3;
        }
        public new bool HandleTask(Context context, ref TaskBranch branch)
        {
            switch((TaskSubtype)branch.currentTask.taskSubtype)
            {
                case TaskSubtype.Like:
                    return HandlingLike(context, ref branch);
                case TaskSubtype.Comment:
                    return HandlingComment(context, ref branch);
                default:
                    log.Error("Stop handle task with unknow taskSubtype;" +
                    " id -> " + branch.currentTask.taskId + ";" +
                    " subtype -> " + branch.currentTask.taskSubtype);
                    return false;
            }
        }
        public bool HandlingComment(Context context,ref TaskBranch branch)
        {  
            MediaGS media = mediaReceiver.GetMediaGS(context, branch.currentUnit, ref branch.session, 1);
            if (media != null)
            {
                TaskData comment = branch.currentTask.taskData.Where(t => t.dataComment != null).First();
                if (CommentMedia(branch, media.mediaPk, comment.dataComment)) 
                {
                    UpdateCommentAction(context, branch.sessionId);
                    CheckOptions(context, ref branch);
                    return true;
                }
            }
            return false;
        }
        public bool HandlingLike(Context context,ref TaskBranch branch)
        {
            if (LikeUserComment(context, ref branch))
            {
                UpdateLikeAction(context, branch.sessionId);
                CheckOptions(context, ref branch); 
                return true;
            }
            return false;   
        }
        public new bool CheckOptions(Context context, ref TaskBranch branch)
        {
            bool optionEnable = branch.currentTask.taskOption.watchStories;
            if(options.WatchStories(ref branch.session, optionEnable, branch.currentUnit.userPk))
            {
                if (optionEnable)
                    UpdateWatchStories(context, branch.sessionId);
                return true;
            }
            return false;
        }
        public bool LikeUserComment(Context context, ref TaskBranch branch)
        {
            var result = api.comment.LikeComment(ref branch.session, branch.currentUnit.commentPk);
            if (result.Succeeded)
            {
                log.Information("Like user's comment by user, id -> " + branch.currentTask.taskId);
                return true;
            }
            else
            {
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, branch.session);
                log.Error("Can't like user's comment, id ->" + branch.currentTask.taskId);
            }
            return false;
        }
        public bool CommentMedia(TaskBranch branch, string mediaPk, string comment)
        {
            var result = api.comment.CommentMedia(ref branch.session, mediaPk, comment);
            if (result.Succeeded)
            {
                log.Information("Comment user's media, id -> " + branch.currentTask.taskId);
                return true;
            }
            else
            {
                if (result.unexceptedResponse)
                    stateHandler.HandleState(result.Info.ResponseType, branch.session);
                log.Error("Can't comment media; id -> " + branch.currentTask.taskId);
            }
            return false;
        }
    }
} 