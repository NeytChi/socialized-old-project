using Serilog.Core;
using InstagramService;
using Newtonsoft.Json.Linq;
using Models.GettingSubscribes;
using Domain.GettingSubscribes;
using UseCases.Tasks;

namespace UseCasesTasks
{
    public class OptionsCondition : BaseHandler, IHandlerGS
    {
        public OptionsCondition(Logger logger): base(logger)
        {
        }
        public new bool handle(JObject json, TaskGS task, ref string message)
        {
            bool success = DefineOptions((TaskType)task.taskType);
            if (success)
            {
                task.taskOption = CreateOptions(ref json, ref message);
                if (task.taskOption != null)
                {
                    success = CheckOptions((TaskType)task.taskType, task.taskOption, ref message);
                }
            }
            else
            {
                success = true;
            }
            if (success && Handler != null)
            {
                return Handler.handle(json, task, ref message);
            }
            Logger.Warning(message);
            return success;
        }
        public bool DefineOptions(TaskType type)
        {
            switch(type)
            {
                case TaskType.Liking:
                case TaskType.Blocking:
                case TaskType.Following:
                case TaskType.Unfollowing:
                    return true; 
                case TaskType.WatchStories:
                case TaskType.Comments:
                default:
                    return false; 
            }
        }
        public TaskOption CreateOptions(ref JObject json, ref string message)
        {
            var tokenOptions = handle(json, "task_options", JTokenType.Object, ref message);
            if (tokenOptions != null) 
            {
                OptionCache cache = tokenOptions.ToObject<OptionCache>();
                return new TaskOption() 
                {
                    dontFollowOnPrivate = cache.dont_follow_on_private,
                    watchStories = cache.watch_stories,
                    likeUsersPost = cache.like_users_post,
                    autoUnfollow = cache.auto_unfollow,
                    unfollowNonReciprocal = cache.unfollow_only_from_non_reciprocal,
                    nextUnlocking = cache.next_unlocking,
                    likesOnUser = cache.likes_on_user
                };

            }
            return null;
        }
        public bool CheckOptions(TaskType type, TaskOption option, ref string message)
        {
            switch(type) 
            { 
                case TaskType.Liking:
                    return CheckLikeOption(option.likesOnUser, ref message); 
                default: return true;
            }
        }
        public bool CheckLikeOption(int likesOnUser, ref string message)
        {
            if (likesOnUser > 0 && likesOnUser < 1000)
            {
                return true;
            }
            else
            {
                message = "Count of likes can't be more that 1000.";
            }
            return false;
        }
    }
}