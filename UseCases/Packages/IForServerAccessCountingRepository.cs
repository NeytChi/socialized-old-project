using Domain.AutoPosting;
using Domain.InstagramAccounts;

namespace UseCases.Packages
{
    public interface IForServerAccessCountingRepository
    {
        ICollection<IGAccount> GetAccounts(long userId, bool accountDeleted = false);
        ICollection<AutoPost> Get(long userId, bool postType);
    }
}
/*

var posts = (from post in context.AutoPosts
    join account in context.IGAccounts on post.sessionId equals account.accountId
    join user in context.Users on account.userId equals user.userId
    where user.userId == userId
        && post.postType
    select post).ToList();


var posts = (from post in context.AutoPosts
    join account in context.IGAccounts on post.sessionId equals account.accountId
    join user in context.Users on account.userId equals user.userId
    where user.userId == userId
        && !post.postType
    select post).ToList();

*/
