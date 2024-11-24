using Serilog;
using System.Linq;
using Serilog.Core;
using Models.Common;
using database.context;
using InstagramService;
using Microsoft.AspNetCore.Mvc;
using Models.SessionComponents;
using System.Collections.Generic;
using Managment;

namespace WebAPI.Controllers
{
    /// <summary>
    /// This class necessary to manage user instagram functional.
    /// </summary>
    [Route("v1.0/InstagramAccounts/[action]/")]
    [ApiController]
    public class IGAccountsController : ControllerBase
    {
        private Context context;
        public SessionManager sessionManager;
        public PackageCondition access;
        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        public IGAccountsController(Context context)
        {
            this.context = context;
            sessionManager = new SessionManager(new Context(false));
            access = new PackageCondition(context, log);
        }
        /// <summary>
        /// Set up instagram login and password for getting session of this account.
        /// </summary>
        [HttpPost]
        [ActionName("Add")]
        public ActionResult<dynamic> Add(InstagramUser cache)
        {
            string message = null;
            User user; IGAccount account;

            if ((user = GetUserByToken(cache.user_token, ref message)) != null)
            {
                if (access.IGAccountsIsTrue(user, ref message))
                {
                    cache.user_id = user.userId;
                    account = sessionManager.AddInstagramSession(cache, ref message);
                    if (account != null)
                        return new { success = true, data = SessionToResponse(account) };
                }
            }
            return Return500Error(message);
        }
        public dynamic SessionToResponse(IGAccount account)
        {
            return new
            {
                authorization = account.State.stateUsable,
                verify_code = account.State.stateChallenger,
                relogin = account.State.stateRelogin,
                session_id = account.accountId
            };
        }
        public User GetUserByToken(string userToken, ref string message)
        {
            User user = context.Users.Where(u
                => u.userToken == userToken
                && u.deleted == false).FirstOrDefault();
            if (user == null)
                message = "Server can't define user.";
            return user;
        }
        /// <summary>
        /// Delete instagram session by user. Set sesssion in deleted state.
        /// </summary>
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult<dynamic> Delete(InstagramUser userData)
        {
            string message = null;
            User user = GetUserByToken(userData.user_token, ref message);
            if (user != null)
            {
                if (sessionManager.DeleteInstagramSession(userData.session_id, user.userId, ref message))
                    return new { success = true, message = "Account was deleted." };
            }
            return Return500Error(message);
        }
        /// <summary>
        /// Verify session by sms verification code.
        /// </summary>
        [HttpPost]
        [ActionName("SmsVerify")]
        public ActionResult<dynamic> SmsVerify(InstagramUser userData)
        {
            string message = null;
            User user = GetUserByToken(userData.user_token, ref message);
            if (user != null)
            {
                if (sessionManager.SmsVerifySession(userData.session_id, user.userId,
                        userData.verify_code, ref message))
                    return new
                    {
                        success = true,
                        data = new
                        {
                            authorization = true,
                            userData.session_id
                        }
                    };
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("SmsAgain")]
        public ActionResult<dynamic> SmsAgain(InstagramUser cache)
        {
            string message = null;
            User user;
            IGAccount account;

            if ((user = GetUserByToken(cache.user_token, ref message)) != null)
            {
                if ((account = sessionManager.GetNonDeleteSession(cache.session_id, user.userId, ref message)) != null)
                {
                    var session = sessionManager.LoadNonDeleteSession(account);
                    if (sessionManager.ChallengeRequired(ref session, true, ref message))
                        return new { success = true };
                }
            }
            return Return500Error(message);
        }
        /// <summary>
        /// Relogin for instagram session.
        /// </summary>
        [HttpPost]
        [ActionName("Relogin")]
        public ActionResult<dynamic> Relogin(InstagramUser cache)
        {
            string message = null;
            User user = GetUserByToken(cache.user_token, ref message);
            if (user != null)
            {
                cache.user_id = user.userId;
                IGAccount account = context.IGAccounts.Where(s
                    => s.accountUsername == cache.instagram_username).FirstOrDefault();
                if (account != null)
                {
                    account.State = context.States.Where(st => st.accountId == account.accountId).First();
                    account = sessionManager.ReloginSession(account, cache, ref message);
                    if (account != null)
                        return new { success = true, data = SessionToResponse(account) };
                }
                else
                    message = "Server can't define ig account by id.";
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("GetSessions")]
        public ActionResult<dynamic> GetSessions(InstagramUser userData)
        {
            string message = null;
            User user = GetUserByToken(userData.user_token, ref message);
            if (user != null)
                return new
                {
                    success = true,
                    data = new { sessions = SelectSessionsToResponce(user.userId) }
                };
            return Return500Error(message);
        }
        public dynamic Return500Error(string message)
        {
            if (Response != null)
                Response.StatusCode = 500;
            log.Warning(message + " IP -> " +
                HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new { success = false, message };
        }
        public dynamic SelectSessionsToResponce(int userId)
        {
            return
            (
                from session in context.IGAccounts
                join state in context.States on session.accountId equals state.accountId
                join business in context.BusinessAccounts on session.accountId equals business.igAccountId into accounts
                where session.userId == userId
                    && session.accountDeleted == false
                select new
                {
                    session_id = session.accountId,
                    session_username = session.accountUsername,
                    created_at = session.createdAt,
                    authorization = session.State.stateUsable,
                    verify_code = session.State.stateChallenger,
                    relogin = session.State.stateRelogin,
                    business_account = accounts.Count() == 0 ? null :
                    new
                    {
                        account_id = accounts.First().businessId,
                        token_created = accounts.First().tokenCreated,
                        received_statistics = accounts.First().received
                    },

                }
            ).ToList();
        }
    }
}