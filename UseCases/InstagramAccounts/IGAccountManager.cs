using Core;
using Domain.InstagramAccounts;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using Serilog;
using UseCases.Exceptions;

namespace UseCases.InstagramAccounts
{
    public class IGAccountManager : BaseManager
    {
        public IInstagramApi Api;
        public IIGAccountRepository AccountRepository;
        public ProfileCondition ProfileCondition = new ProfileCondition();

        public IGAccountManager(ILogger logger, 
            IInstagramApi api, 
            IIGAccountRepository accountRepository) : base(logger)
        {
            Api = api;
            AccountRepository = accountRepository;
        }
        public InstaLoginResult LoginSession(Session session)
        {
            string message = "";
            var result = Api.Login(session);
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
        public Session RestoreInstagramSession(IGAccount account)
        {
            var decryptedSessionSave = ProfileCondition.Decrypt(account.State.SessionSave);
            var session = Api.LoadStateDataFromString(decryptedSessionSave);
            Logger.Information($"Інстаграм сесія була востановлена з тексту, id={account.Id}.");
            return session;
        }
    }
}
