using Serilog;
using System.Text;
using System.Security.Cryptography;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using Domain.SessionComponents;
using Domain.GettingSubscribes;

namespace UseCases.AutoPosts
{
    public class SessionManager
    {
        private ILogger Logger;

        public SessionManager(ILogger logger)
        {
            Logger = logger;
            context = context;
            api = InstagramApi.GetInstance();
            hash = Program.serverConfiguration().GetValue<string>("md5_hash");
        }
        public string hash = "1234pass";
        public Context context;
        public InstagramApi api;

        public IGAccount AddInstagramSession(InstagramUser user, ref string message)
        {
            if (!string.IsNullOrEmpty(user.instagram_username) && !string.IsNullOrEmpty(user.instagram_password))
            {
                user.instagram_username = user.instagram_username.Trim();

                var account = context.IGAccounts.Where(s
                    => s.userId == user.user_id
                    && s.accountUsername == user.instagram_username).FirstOrDefault();
                if (account == null)
                {
                    return SetupSession(user, ref message);
                }
                else
                {
                    account.State = context.States.Where(st => st.accountId == account.accountId).First();
                    return RecoverySession(account, user, ref message);
                }
            }
            else
                message = "Username or password is empty.";
            return null;
        }
        public IGAccount RecoverySession(IGAccount account, InstagramUser cache, ref string message)
        {
            if (account.accountDeleted)
            {
                account.State.stateRelogin = true;
                context.States.Update(account.State);
                context.SaveChanges();
                return RestoreSession(account, cache, ref message);
            }
            message = "User has already this account.";
            return null;
        }
        public IGAccount RestoreSession(IGAccount account, InstagramUser user, ref string message)
        {
            account = ReloginSession(account, user, ref message);
            if (account != null)
            {
                account.userId = user.user_id;
                account.accountDeleted = false;
                context.IGAccounts.Update(account);
                context.SaveChanges();
                Logger.Information("Session was successfully restored");
            }
            return account;
        }
        /// <summary>
        /// Create instagram session for current user, login with it and save if it success.
        /// <summary>  
        public IGAccount SetupSession(InstagramUser cache, ref string message)
        {
            var session = new Session(cache.instagram_username, cache.instagram_password);
            session.userId = cache.user_id;
            var loginResult = LoginSession(session, ref message);
            if (loginResult == InstaLoginResult.Success)
            {
                return SaveSession(session, false, ref message);
            }
            if (loginResult == InstaLoginResult.ChallengeRequired)
            {
                if (ChallengeRequired(ref session, false, ref message))
                {
                    return SaveSession(session, true, ref message);
                }
            }
            return null;
        }
        public bool ChallengeRequired(ref Session session, bool replay, ref string message)
        {
            var challenge = api.GetChallengeRequireVerifyMethod(ref session);
            if (challenge.Succeeded)
            {
                var result = api.VerifyCodeToSMSForChallengeRequire(replay, ref session);
                if (result.Succeeded)
                {
                    message = "Add session with challenge required state.";
                    return true;
                }
                else
                {
                    Logger.Warning("Can't perform verify code to sms challenge require method.");
                }
            }
            else
            {
                Logger.Warning("Can't get challenge require verify method.");
            }
            return false;
        }
        public IGAccount SaveSession(Session session, bool challengeRequired, ref string message)
        {
            if (context.IGAccounts.Where(s
                    => s.userId == session.userId
                    && s.accountUsername == session.User.UserName).FirstOrDefault() == null)
            {
                var state = new SessionState();
                state.stateChallenger = challengeRequired;
                state.stateUsable = challengeRequired ? false : true;

                var timeAction = new TimeAction();
                timeAction.accountOld = false;

                var account = new IGAccount()
                {
                    userId = session.userId,
                    accountUsername = session.User.UserName,
                    createdAt = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                    State = state,
                    timeAction = timeAction,
                };
                context.IGAccounts.Add(account);
                context.SaveChanges();

                session.sessionId = account.accountId;
                account.sessionSave = Encrypt(api.GetStateDataAsString(ref session));
                context.IGAccounts.Update(account);
                context.SaveChanges();

                Logger.Information("Server save session, id -> " + account.accountId);
                return account;
            }
            message = "User has already this account.";
            return null;
        }
        /// <summary>
        /// Relogin instagram session. Load non-deleted session and try to login with it.
        /// <summary>  
        public IGAccount ReloginSession(IGAccount account, InstagramUser user, ref string message)
        {
            if (account.State.stateRelogin)
            {
                var session = LoadNonDeleteSession(account);
                session.User.Password = user.instagram_password;
                session.requestMessage.Password = user.instagram_password;
                var loginResult = LoginSession(session, ref message);
                if (loginResult == InstaLoginResult.Success)
                {
                    UpdateUsableSession(session, account);
                    return account;
                }
                if (loginResult == InstaLoginResult.ChallengeRequired)
                {
                    ChallengeRequired(ref session, true, ref message);
                    UpdateChallengedSession(session, account);
                    return account;
                }
            }
            else
            {
                message = "Session doesn't have relogin state.";
            }
            Logger.Warning(message);
            return null;
        }
        public void UpdateUsableSession(Session session, IGAccount account)
        {
            account.sessionSave = Encrypt(api.GetStateDataAsString(ref session));
            account.State.stateUsable = true;
            account.State.stateRelogin = false;
            account.State.stateChallenger = false;
            context.IGAccounts.Update(account);
            context.States.Update(account.State);
            context.SaveChanges();
        }
        public void UpdateChallengedSession(Session session, IGAccount account)
        {
            account.sessionSave = Encrypt(api.GetStateDataAsString(ref session));
            account.State.stateUsable = false;
            account.State.stateRelogin = false;
            account.State.stateChallenger = true;
            context.IGAccounts.Update(account);
            context.States.Update(account.State);
            context.SaveChanges();
        }
        public InstaLoginResult LoginSession(Session session, ref string message)
        {
            var loginResult = api.Login(ref session);
            switch (loginResult)
            {
                case InstaLoginResult.Success:
                    message = "Session was successfully login.";
                    break;
                case InstaLoginResult.ChallengeRequired:
                    message = "Session has challenge required state";
                    break;
                case InstaLoginResult.TwoFactorRequired:
                    message = "User account has two factor authentication.";
                    break;
                case InstaLoginResult.InactiveUser:
                    message = "Inactive user account.";
                    break;
                case InstaLoginResult.InvalidUser:
                    message = "Invalid user.";
                    break;
                case InstaLoginResult.BadPassword:
                    message = "Bad password.";
                    break;
                case InstaLoginResult.LimitError:
                case InstaLoginResult.Exception:
                default:
                    message = "Unknow instagram login exception. Message:" + loginResult.ToString() + ".";
                    break;
            }
            return loginResult;
        }
        /// <summary>
        /// Call instagram API request to verify session by verifycation code.
        /// <summary>
        public bool SmsVerifySession(long sessionId, int userId, string verifyCode, ref string message)
        {
            IGAccount account;
            Session session;

            if (!string.IsNullOrEmpty(verifyCode))
            {
                if ((account = GetNonDeleteSession(sessionId, userId, ref message)) != null)
                {
                    if (account.State.stateChallenger == true)
                    {
                        session = LoadNonDeleteSession(account);
                        var loginResult = api.VerifyCodeForChallengeRequire(verifyCode, ref session);
                        if (loginResult == InstaLoginResult.Success)
                        {
                            UpdateUsableSession(session, account);
                            Logger.Information("Verified challenged session, id -> " + account.accountId);
                            return true;
                        }
                        else
                        {
                            message = "Invalid confirmation code.";
                        }
                    }
                    else
                    {
                        message = "Session doesn't have challange required state.";
                    }
                }
            }
            else
            {
                message = "Verify code is null or empty.";
            }
            Logger.Warning(message);
            return false;
        }
        public IGAccount GetNonDeleteSession(long accountId, int userId, ref string message)
        {
            IGAccount account = context.IGAccounts.Where(s
                => s.accountId == accountId
                && s.userId == userId
                && s.accountDeleted == false).FirstOrDefault();
            if (account == null)
            {
                message = "Server can't define ig account by account id & user id.";
                Logger.Warning(message);
            }
            else
                account.State = context.States.Where(st
                    => st.accountId == accountId).First();
            return account;
        }
        public IGAccount GetUsableSession(string userToken, long accountId)
        {
            IGAccount account = (from s in context.IGAccounts
                                 join u in context.Users on s.userId equals u.userId
                                 join st in context.States on s.accountId equals st.accountId
                                 where u.userToken == userToken
                                     && s.accountId == accountId
                                     && s.accountDeleted == false
                                     && st.stateUsable == true
                                 select s).FirstOrDefault();
            if (account == null)
                Logger.Warning("Server can't define usable ig account; id -> " + accountId);
            return account;
        }
        public IGAccount GetUsableSession(long sessionId)
        {
            IGAccount session = (from s in context.IGAccounts
                                 join st in context.States on s.accountId equals st.accountId
                                 where s.accountId == sessionId
                                     && s.accountDeleted == false
                                     && st.stateUsable == true
                                 select s).FirstOrDefault();
            if (session == null)
                Logger.Warning("Server can't define usable ig account, id ->" + sessionId);
            return session;
        }
        public bool DeleteInstagramSession(long sessionId, int userId, ref string message)
        {
            IGAccount account = GetNonDeleteSession(sessionId, userId, ref message);
            if (account != null)
            {
                EndSessionsTask(sessionId);
                account.accountDeleted = true;
                context.IGAccounts.Update(account);
                context.SaveChanges();
                Logger.Information("Delete user's instagram account, id ->" + userId);
                return true;
            }
            return false;
        }
        public void EndSessionsTask(long sessionId)
        {
            List<TaskGS> tasks = context.TaskGS.Where(t
                => t.sessionId == sessionId).ToList();
            foreach (TaskGS task in tasks)
            {
                task.taskDeleted = true;
                task.taskStopped = true;
                task.taskUpdated = true;
            }
            context.TaskGS.UpdateRange(tasks);
            context.SaveChanges();
            Logger.Information("Delete all session's tasks.");
        }
        public Session LoadNonDeleteSession(IGAccount cache)
        {
            Session session = new Session();
            api.LoadStateDataFromString(Decrypt(cache.sessionSave), ref session);
            session.sessionId = cache.accountId;
            session.userId = cache.userId;
            Logger.Information("Load session to server, id -> " + cache.userId);
            return session;
        }
        public Session LoadSession(long sessionId)
        {
            IGAccount account = GetUsableSession(sessionId);
            if (account != null)
            {
                Session session = new Session();
                api.LoadStateDataFromString(Decrypt(account.sessionSave), ref session);
                session.sessionId = account.accountId;
                session.userId = account.userId;
                Logger.Information("Load session to server, id -> " + account.accountId);
                return session;
            }
            return null;
        }
        /// <summary>
        /// Function for receiving instagram profile account data.
        /// <param> Session id need to be long type of variable.</param>
        /// <summary>
        public void StartHandleInstagramProfile(object sessionId)
        {
            Session session = LoadSession((long)sessionId);
            TimeAction timeAction = context.timeAction.Where(a => a.accountId == session.sessionId).First();
            IResult<InstaWebAccountInfo> accountInfo = api.web.GetAccountInfo(ref session);
            if (accountInfo.Succeeded)
            {
                timeAction.accountOld = accountInfo.Value.JoinedDate > DateTime.Now.AddMonths(-6);
                context.timeAction.Attach(timeAction).Property(t => t.accountOld).IsModified = true; ;
                context.SaveChanges();
                Logger.Information("Server get account details by sessionId, id -> " + sessionId);
            }
            else
            {
                if (accountInfo.unexceptedResponse)
                {
                    //stateHandler.HandleState(accountInfo.unexceptedResponse, 
                    //accountInfo.Info.ResponseType, session);
                }
                Logger.Warning("Server can't get account details by sessionId -> " + sessionId);
            }
        }
        public string Decrypt(string toDectypt)
        {
            var data = Convert.FromBase64String(toDectypt);
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var keys = md5.ComputeHash(Encoding.UTF8.GetBytes(hash));
                using (var tripDes = new TripleDESCryptoServiceProvider()
                {
                    Key = keys,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7
                })
                {
                    var transform = tripDes.CreateDecryptor();
                    var results = transform.TransformFinalBlock(data, 0, data.Length);
                    return Encoding.UTF8.GetString(results);
                }
            }
        }
        public string Encrypt(string toEncrypt)
        {
            byte[] results;

            var data = Encoding.UTF8.GetBytes(toEncrypt);
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var keys = md5.ComputeHash(Encoding.UTF8.GetBytes(hash));
                using (var tripDes = new TripleDESCryptoServiceProvider()
                {
                    Key = keys,
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7
                })
                {
                    var transform = tripDes.CreateEncryptor();
                    results = transform.TransformFinalBlock(data, 0, data.Length);
                    return Convert.ToBase64String(results, 0, results.Length);
                }
            }
        }
    }
}