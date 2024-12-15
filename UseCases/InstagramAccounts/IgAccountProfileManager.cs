using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.InstagramAccounts
{
    public class IgAccountProfileManager : BaseManager
    {
        public IgAccountProfileManager(ILogger logger) : base(logger)
        {

        }
        /// <summary>
        /// Function for receiving instagram profile account data.
        /// <param> Session id need to be long type of variable.</param>
        /// <summary>
        public void StartHandleInstagramProfile(long sessionId)
        {
            var session = RestoreInstagramSession(sessionId);
            var timeAction = context.timeAction.Where(a => a.accountId == session.sessionId).First();
            var accountInfo = api.web.GetAccountInfo(ref session);
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
    }
}
