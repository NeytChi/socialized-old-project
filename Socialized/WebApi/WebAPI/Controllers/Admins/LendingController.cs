using System;
using Common;
using Serilog;
using System.Linq;
using Models.Lending;
using database.context;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers.Admins
{
    [Route("v1.0/[controller]/[action]/")]
    [ApiController]
    public class LendingController : ControllerResponseHandler
    {
        ProfileCondition val;

        public LendingController()
        {
            val = new ProfileCondition(log);
        }
        [HttpPost]
        [ActionName("followto")]
        public ActionResult<dynamic> FollowTo(FollowerCache cache)
        {
            string message = null;

            Follower follower = GetFollowerByEmail(cache.follower_email, ref message);
            if (follower == null)
                if (val.EmailIsTrue(cache.follower_email, ref message))
                {
                    AddFollower(cache.follower_email, FindFollowerFromUsers(cache.follower_email));
                    return new { success = true };
                }
            return StatusCode500(message);
        }
        public int FindFollowerFromUsers(string followerEmail)
        {
            return context.Users.Where(u
                => u.userEmail == followerEmail
                && u.deleted == false
                && u.activate == true).Select(u => u.userId).FirstOrDefault();
        }
        public Follower GetFollowerByEmail(string email, ref string message)
        {
            Follower follower = context.Followers.Where(f => f.followerEmail == email).FirstOrDefault();
            if (follower != null)
            {
                log.Information("Follower with this email already exist");
                message = "exist_email";
            }
            return follower;
        }
        public void AddFollower(string email, int userId)
        {
            Follower follower = new Follower();
            follower.userId = userId;
            follower.followerEmail = email;
            follower.createdAt = DateTime.Now;
            follower.enableMailing = true;
            context.Add(follower);
            context.SaveChanges();
        }
    }
}