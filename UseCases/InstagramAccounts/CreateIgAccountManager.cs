using Core;
using Serilog;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.API.Builder;
using Domain.InstagramAccounts;
using UseCases.Exceptions;
using UseCases.InstagramAccounts.Commands;

namespace UseCases.InstagramAccounts
{
    public interface IIGAccountManager
    {

    }
    public class CreateIGAccountManager : IGAccountManager, IIGAccountManager
    {
        public CreateIGAccountManager(ILogger logger, 
            IIGAccountRepository accountRepository,
            IInstagramApi api) : base(logger, api, accountRepository)
        {

        }
        public IGAccount Create(CreateIgAccountCommand command)
        {
            command.InstagramUserName = command.InstagramUserName.Trim();

            var account = AccountRepository.GetByWithState(command.UserToken, command.InstagramUserName);
            if (account != null)
            {
                return RecoverySession(account, command);
            }
            var session = new Session(command.InstagramUserName, command.InstagramPassword);
            session.userId = account.UserId;
            var loginResult = LoginSession(session);
            if (loginResult == InstaLoginResult.Success)
            {
                return SaveSession(session, false);
            }
            if (loginResult == InstaLoginResult.ChallengeRequired)
            {
                ChallengeRequired(session, false);
                return SaveSession(session, true);
            }
            return null;
        }
        public IGAccount RecoverySession(IGAccount account, IgAccountRequirements requirements)
        {
            account.State.Relogin = true;

            var session = RestoreInstagramSession(account);
            
            session.User.Password = requirements.InstagramPassword;
            session.requestMessage.Password = requirements.InstagramPassword;
            
            var loginResult = LoginSession(session);
            if (loginResult == InstaLoginResult.Success)
            {
                var stateData = Api.GetStateDataAsString(session);
                account.State.SessionSave = ProfileCondition.Encrypt(stateData);
                account.State.Usable = true;
                account.State.Relogin = false;
                account.State.Challenger = false;
                AccountRepository.Update(account);
                return account;
            }
            if (loginResult == InstaLoginResult.ChallengeRequired)
            {
                ChallengeRequired(session, true);
                var stateData = Api.GetStateDataAsString(session);
                account.State.SessionSave = ProfileCondition.Encrypt(stateData);
                account.State.Usable = false;
                account.State.Relogin = false;
                account.State.Challenger = true;
                AccountRepository.Update(account);
                return account;
            }

            account.IsDeleted = false;
            AccountRepository.Update(account);

            Logger.Information($"Сесія була востановлена, id={account.Id}");
            return account;
        }
        public void ChallengeRequired(Session session, bool replay)
        {
            var challenge = Api.GetChallengeRequireVerifyMethod(session);
            if (!challenge.Succeeded)
            {
                throw new IgAccountException("Сервер не може підтвердити Instagram аккаунт.");
            }
            var result = Api.VerifyCodeToSMSForChallengeRequire(replay, session);
            if (!result.Succeeded)
            {
                throw new IgAccountException("Сервер не може запустити верифікації аккаунту через SMS код.");
            }
            Logger.Information("Сесія Instagram аккаунту була пройдена через процедуру підтвердження.");
        }
        public IGAccount SaveSession(Session session, bool challengeRequired)
        {
            var account = AccountRepository.GetByWithState(session.userId, session.User.UserName);
            if (account != null)
            {
                throw new ValidationException("Користувач вже має такий самий аккаунт.");
            }
            account = new IGAccount()
            {
                UserId = session.userId,
                Username = session.User.UserName,
                CreatedAt = DateTime.UtcNow,
                State = new SessionState
                {
                    Challenger = challengeRequired,
                    Usable = challengeRequired ? false : true
                }
            };
            AccountRepository.Create(account);
            session.sessionId = account.Id;
            var stateData = Api.GetStateDataAsString(session);
            var encryptData = ProfileCondition.Encrypt(stateData);
            account.State.SessionSave = encryptData;
            AccountRepository.Update(account);
            Logger.Information($"Сесія Instagram аккаунту було збережено, id={account.Id}.");
            return account;
        }
        public Session RestoreInstagramSession(long accountId)
        {
            var account = AccountRepository.Get(accountId);
            if (account == null)
            {
                throw new NotFoundException("Сервер не визначив запис Інстаграм аккаунту по id.");
            }
            var decryptedSession = ProfileCondition.Decrypt(account.State.SessionSave);
            var session = Api.LoadStateDataFromString(decryptedSession);
            Logger.Information($"Сессія Інстаграм аккаунту була востановлена, id={account.Id}.");
            return session;
        }
    }
}