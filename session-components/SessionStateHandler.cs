using System;
using Serilog;
using System.Linq;
using Serilog.Core;
using database.context;
using System.Threading;
using Models.SessionComponents;
using InstagramApiSharp.Classes;
using System.Collections.Generic;
using InstagramApiSharp.API.Builder;
using Managment;

namespace InstagramService
{
    /// <summary>
    /// This class handle session's state by unexcepted response of Instagram service.
    /// </summary>
    public class SessionStateHandler
    {
        Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        // private Timer timer;
        public Context contextRecovery;
        public SessionStateHandler(Context context)
        {
            contextRecovery = context;
            CheckRecoverySession(null);
        }
        public void CheckRecoverySession(object session)
        {
            List<SessionState> states = contextRecovery.States.Where(st 
                => st.stateUsable == false
                && st.stateSpammed == true
                && st.spammedEnd > DateTime.Now).ToList();
            foreach(SessionState state in states) {
                state.stateUsable = true;
                state.stateSpammed = false;
                contextRecovery.Update(state);
            }
            contextRecovery.SaveChanges();
            //timer = new Timer(CheckRecoverySession, null, 60 * 2 * 1000, Timeout.Infinite);
        }
        /// <summary>
        /// Handle input response and session.
        /// </summary>
        /// <param> Response from Instagram service.</param>
        /// <param> Session, which belongs to responce.</param>
        public void HandleState(ResponseType responseType, Session session)
        {
            IGAccount account = GetSessionWithoutUsable(session.sessionId);
            switch (responseType) {
                case ResponseType.SentryBlock:
                    HandleSpam(ref account, DateTime.Now.AddYears(1));
                    break;
                case ResponseType.Spam:
                    HandleSpam(ref account, DateTime.Now.AddDays(3));
                    break;
                case ResponseType.RequestsLimit:
                    HandleSpam(ref account, DateTime.Now.AddHours(1));
                    break;
                case ResponseType.ActionBlocked:
                    HandleSpam(ref account, DateTime.Now.AddHours(1));
                    break;
                case ResponseType.InactiveUser:
                case ResponseType.LoginRequired:
                    HandleRelogin(ref account);
                    break;
                case ResponseType.CantLike:
                case ResponseType.DeletedPost:
                case ResponseType.AlreadyLiked:
                case ResponseType.MediaNotFound:
                case ResponseType.ConsentRequired:
                case ResponseType.CommentingIsDisabled:
                    HandleMediaErrors(ref account, responseType);
                    break;
                case ResponseType.ChallengeRequired:
                    HandleChallengeRequired(ref account, ref session);
                    break;
                case ResponseType.CheckPointRequired:
                default:
                    log.Warning("Unknown session's behavior, id -> " + account.accountId);
                    break;
            }
            log.Information("Session state with unexcepted was handled, id -> " + account.accountId);
        } 
        public void HandleChallengeRequired(ref IGAccount account, ref Session session)
        {
            string message = null;
            using (Context context = new Context(false)) {
                account.State.stateUsable = false;
                account.State.stateChallenger = true;
                context.States.Update(account.State);
                context.SaveChanges();
                new SessionManager(context).ChallengeRequired(ref session, true, ref message);
            }
            log.Information("Session was set in challenged state, id -> " + account.accountId);
        }
        public void HandleRelogin(ref IGAccount account)
        {
            using (Context context = new Context(false)) {
                account.State.stateUsable = false;
                account.State.stateRelogin = true;
                context.States.Update(account.State);
                context.SaveChanges();
            }
            log.Information("Session was set in relogin state, id -> " + account.accountId);
        }
        public void HandleSpam(ref IGAccount account, DateTime spammedEnd)
        {
            using (Context context = new Context(false))
            {
                account.State.stateUsable = false;
                account.State.stateSpammed = true;
                account.State.spammedStarted = DateTime.Now;
                account.State.spammedEnd = spammedEnd;
                context.States.Update(account.State);
                context.SaveChanges();
            }
            log.Information("Session was set in spammed state, id -> " + account.accountId);
        }
        public void HandleMediaErrors(ref IGAccount account, ResponseType type)
        {
            log.Information("Session get media error; responseType -> " + type + " ; id -> " + account.accountId);
        }
        public IGAccount GetNonDeleteSession(long sessionId)
        {
            using (Context context = new Context(false)) {
                IGAccount account = context.IGAccounts.Where(save => save.accountId == sessionId
                && save.accountDeleted == false).FirstOrDefault();
                if (account != null) {
                    account.State = context.States.Where(st => st.accountId == account.accountId).First();
                    if (account.State.stateUsable) {
                        log.Information("Get account, id -> " + account.accountId);
                        return account;
                    }
                    else
                        log.Information("Can't get session, session isn't usable, id ->" + account.accountId);
                }
                else
                    log.Information("Can't define session, id -> " + sessionId);
            }
            return null;    
        }
        public IGAccount GetUsable(Context context, long accountId)
        {
            IGAccount account = context.IGAccounts.Where(save 
                => save.accountId == accountId
                && save.accountDeleted == false).FirstOrDefault();
            if (account != null) {
                account.State = context.States.Where(st 
                    => st.accountId == account.accountId).First();
                if (account.State.stateUsable) {
                    log.Information("Get ig account, id -> " + account.accountId);
                    return account;
                }
                else
                    log.Information("Can't get ig account, isn't usable, id -> " + account.accountId);
            }
            else
                log.Information("Can't define account, id -> " + accountId);
            return null;    
        }
        public IGAccount GetSessionWithoutUsable(long accountId)
        {
            using (Context context = new Context(false))
            {
                IGAccount account = context.IGAccounts.Where(save => save.accountId == accountId).FirstOrDefault();
                if (account != null) {
                    account.State = context.States.Where(st => st.accountId == account.accountId).First();
                    log.Information("Get unusable session, id -> " + account.accountId);
                    return account;
                }
            }
            return null;
        }   
    }
}