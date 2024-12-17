using Serilog;
using Core;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using UseCases.Exceptions;
using Domain.InstagramAccounts;

namespace UseCases.InstagramAccounts
{
    public class SmsVerifyIgAccountManager : IGAccountManager
    {
        public SmsVerifyIgAccountManager(ILogger logger, 
            IInstagramApi api, 
            IIGAccountRepository accountRepository) : base (logger, api, accountRepository)
        {

        }
        public void SmsVerifySession(long accountId, int userId, string verifyCode)
        {
            var account = AccountRepository.GetByWithState(accountId);
            if (account == null)
            {
                throw new NotFoundException("Сервер не визначив запис Instagram аккаунту по id.");
            }
            if (!account.State.Challenger)
            {
                throw new NotFoundException("Сесія Instagram аккаунту не потребує підтвердження аккаунту.");
            }
            var session = RestoreInstagramSession(account);
            var loginResult = Api.VerifyCodeForChallengeRequire(verifyCode, session);
            if (loginResult != InstaLoginResult.Success)
            {
                throw new IgAccountException("Код підвердження Instagram аккаунту не вірний.");
            }
            var stateData = Api.GetStateDataAsString(session);
            account.State.SessionSave = ProfileCondition.Encrypt(stateData);
            account.State.Usable = true;
            account.State.Relogin = false;
            account.State.Challenger = false;
            AccountRepository.Update(account);
            Logger.Information($"Сесія Instagram аккаунту було веріфікована, id={account.Id}.");
        }
    }
}
