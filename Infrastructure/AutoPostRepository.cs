using Domain.AutoPosting;

namespace Infrastructure
{
    public class AutoPostRepository
    {
        private Context Context;
        public AutoPostRepository(Context context)
        {
            Context = context;
        }
        public List<AutoPost> GetBy(DateTime executeAt,
            bool postExecuted = false,
            bool postDeleted = false)
        {
            return Context.AutoPosts.Where(a => a.Executed == postExecuted && executeAt > a.ExecuteAt && a.Deleted == postDeleted).OrderBy(a => a.ExecuteAt).ToList();
        }
        public AutoPost GetBy(string userToken, long postId, bool postDeleted = false)
        {
            return (from autoPost in Context.AutoPosts
                    join account in Context.IGAccounts on autoPost.AccountId equals account.Id
                    join user in Context.Users on account.UserId equals user.Id
                    where user.TokenForUse == userToken
                        && autoPost.postId == postId
                        && autoPost.postDeleted == postDeleted
                    select autoPost).FirstOrDefault();
        }
        public AutoPost GetBy(string userToken, long postId, bool postDeleted, bool postAutoDeleted, bool postExecuted)
        {
            return (from p in Context.AutoPosts
                    join s in Context.IGAccounts on p.sessionId equals s.accountId
                    join u in Context.Users on s.userId equals u.userId
                    where u.userToken == userToken
                        && p.postId == postId
                        && p.postDeleted == postDeleted
                        && p.postAutoDeleted == postAutoDeleted
                        && p.postExecuted == postExecuted
                    select p).FirstOrDefault();
        }
        public ICollection<AutoPost> GetBy(GetAutoPostsCommand command)
        {
            return (from p in Context.AutoPosts
                    join s in Context.IGAccounts on p.sessionId equals s.accountId
                    join u in Context.Users on s.userId equals u.userId
                    join f in Context.AutoPostFiles on p.postId equals f.postId into files
                    where u.userToken == command.UserToken
                        && s.accountId == command.SessionId
                        && p.postExecuted == command.PostExecuted
                        && p.postDeleted == command.PostDeleted
                        && p.postAutoDeleted == command.PostAutoDeleted
                        && p.executeAt > command.From
                        && p.executeAt < command.To
                    orderby p.postId descending
                    select p )
                    .Skip(command.Since * command.Count).Take(command.Count).ToList();
        }
    }
}
/*
 public ICollection<AutoPost> GetBy(GetAutoPostsCommand command)  
Structure of output from this method
    new
    {
        post_id = p.postId,
        post_type = p.postType,
        created_at = p.createdAt,
        execute_at = p.executeAt.AddHours(p.timezone),
        auto_delete = p.autoDelete,
        delete_after = p.autoDelete ? p.deleteAfter.AddHours(p.timezone) : p.deleteAfter,
        post_location = p.postLocation,
        post_description = p.postDescription,
        post_comment = p.postComment,
        p.timezone,
        category_id = p.categoryId,
        category_name = p.categoryId == 0 ? ""
            : context.Categories.Where(x => x.categoryId == p.categoryId
                && !x.categoryDeleted).FirstOrDefault().categoryName ?? "",
        category_color = p.categoryId == 0 ? ""
            : context.Categories.Where(x => x.categoryId == p.categoryId
                && !x.categoryDeleted).FirstOrDefault().categoryColor ?? "",
        files = GetPostFilesToOutput(files)
    }
 */