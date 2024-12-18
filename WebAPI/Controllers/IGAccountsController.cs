using Serilog;
using Serilog.Core;
using Microsoft.AspNetCore.Mvc;
using UseCases.InstagramAccounts;
using UseCases.InstagramAccounts.Commands;
using WebAPI.Responses;

namespace WebAPI.Controllers
{
    public class IGAccountsController : ControllerResponseBase
    {
        private IIGAccountManager AccountManager;
        private IDeleteIgAccountManager DeleteIgAccountManager;
        private ISmsVerifyIgAccountManager SmsVerifyAccountManager;

        public IGAccountsController(IIGAccountManager accountManager, 
            IDeleteIgAccountManager deleteIgAccountManager,
            ISmsVerifyIgAccountManager smsVerify)
        {
            AccountManager = accountManager;
            DeleteIgAccountManager = deleteIgAccountManager;
            SmsVerifyAccountManager = smsVerify;
        }
        [HttpPost]
        public ActionResult<DataResponse> Create(CreateIgAccountCommand command)
        {
            var result = AccountManager.Create(command);
            
            return new DataResponse(true, result);
        }   
        [HttpDelete]
        public ActionResult<SuccessResponse> Delete(DeleteIgAccountCommand command)
        {
            DeleteIgAccountManager.Delete(command.AccountId);

            return new SuccessResponse(true);
        }
        
        [HttpPost]
        [ActionName("SmsVerify")]
        public ActionResult<SuccessResponse> SmsVerify(SmsVefiryIgAccountCommand command)
        {
            SmsVerifyAccountManager.SmsVerifySession(command);

            return new SuccessResponse(true);
        }
        /*
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
       
        [HttpGet]
        public ActionResult<dynamic> GetAccounts(InstagramUser userData)
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
        } */
    }
}