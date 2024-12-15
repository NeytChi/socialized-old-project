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
    public class IGAccountManager : BaseManager, IIGAccountManager
    {
        private IIGAccountRepository AccountRepository;
        private ITaskGettingSubscribesRepository TaskGettingSubscribesRepository;
        private ProfileCondition ProfileCondition = new ProfileCondition();
        private InstagramApi api;

        public IGAccountManager(ILogger logger, 
            IIGAccountRepository accountRepository,
            ITaskGettingSubscribesRepository taskGettingSubscribesRepository) : base(logger)
        {
            AccountRepository = accountRepository;
            TaskGettingSubscribesRepository = taskGettingSubscribesRepository;
            api = InstagramApi.GetInstance();
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
                var stateData = api.GetStateDataAsString(session);
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
                var stateData = api.GetStateDataAsString(session);
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
            var challenge = api.GetChallengeRequireVerifyMethod(ref session);
            if (!challenge.Succeeded)
            {
                throw new IgAccountException("Сервер не може підтвердити Instagram аккаунт.");
            }
            var result = api.VerifyCodeToSMSForChallengeRequire(replay, ref session);
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
            var stateData = api.GetStateDataAsString(session);
            var encryptData = ProfileCondition.Encrypt(stateData);
            account.State.SessionSave = encryptData;
            AccountRepository.Update(account);
            Logger.Information($"Сесія Instagram аккаунту було збережено, id={account.Id}.");
            return account;
        }
        public InstaLoginResult LoginSession(Session session)
        {
            string message = "";
            var result = api.Login(ref session);
            switch (result)
            {
                case InstaLoginResult.Success:
                    message = "Сесія Instagram аккаунт був успішно залогінен.";
                    return result;
                case InstaLoginResult.ChallengeRequired:
                    message = "Сесія Instagram аккаунту потребує підтвердження по коду.";
                    break;
                case InstaLoginResult.TwoFactorRequired:
                    message = "Сесія Instagram аккаунту потребує проходження двох-факторної організації.";
                    break;
                case InstaLoginResult.InactiveUser:
                    message = "Сесія Instagram аккаунту не активна.";
                    break;
                case InstaLoginResult.InvalidUser:
                    message = "Правильно введені данні для входу в аккаунт.";
                    break;
                case InstaLoginResult.BadPassword:
                    message = "Неправильний пароль.";
                    break;
                case InstaLoginResult.LimitError:
                case InstaLoginResult.Exception:
                default:
                    message = $"Невідома помилка при спробі зайти(логін) в Instagram аккаунт. Виключення:{result.ToString()}.";
                    break;
            }
            throw new IgAccountException(message);
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
            var loginResult = api.VerifyCodeForChallengeRequire(verifyCode, ref session);
            if (loginResult != InstaLoginResult.Success)
            {
                throw new IgAccountException("Код підвердження Instagram аккаунту не вірний.");
            }
            var stateData = api.GetStateDataAsString(session);
            account.State.SessionSave = ProfileCondition.Encrypt(stateData);
            account.State.Usable = true;
            account.State.Relogin = false;
            account.State.Challenger = false;
            AccountRepository.Update(account);
            Logger.Information($"Сесія Instagram аккаунту було веріфікована, id={account.Id}.");
        }
        public void Delete(long accountId)
        {
            var account = AccountRepository.GetByWithState(accountId);
            if (account == null)
            {
                throw new NotFoundException("Сервер не визначив запис Instagram аккаунту по id.");
            }
            account.IsDeleted = true;
            AccountRepository.Update(account);
            Logger.Information($"Instagram аккаунт був видалений, id={accountId}.");
            StopTasksGettingSubscribes(accountId);
        }
        public void StopTasksGettingSubscribes(long accountId)
        {
            var tasks = TaskGettingSubscribesRepository.GetBy(accountId);
            foreach (var task in tasks)
            {
                task.Deleted = true;
                task.Stopped = true;
                task.Updated = true;
            }
            TaskGettingSubscribesRepository.Update(tasks);
            Logger.Information($"Всі задачі були закриті по Instagram аккаунту, id={accountId}.");
        }
        public Session RestoreInstagramSession(IGAccount account)
        {
            var decryptedSessionSave = ProfileCondition.Decrypt(account.State.SessionSave);
            var session = api.LoadStateDataFromString(decryptedSessionSave);
            Logger.Information($"Інстаграм сесія була востановлена з тексту, id={account.Id}.");
            return session;
        }
        public Session RestoreInstagramSession(long accountId)
        {
            var account = AccountRepository.Get(accountId);
            if (account == null)
            {
                throw new NotFoundException("Сервер не визначив запис Інстаграм аккаунту по id.");
            }
            var decryptedSession = ProfileCondition.Decrypt(account.State.SessionSave);
            var session = api.LoadStateDataFromString(decryptedSession);
            Logger.Information($"Сессія Інстаграм аккаунту була востановлена, id={account.Id}.");
            return session;
        }
    }
}