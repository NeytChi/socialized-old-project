using Serilog;
using System.Web;
using Domain.Admins;
using Domain.AutoPosting;
using Domain.Users;
using UseCases.Exceptions;
using UseCases.Appeals.Commands;
using Domain.Appeals;

namespace UseCases.Appeals
{
    public interface IAppealManager
    {
        Appeal Create(CreateAppealCommand command);
        ICollection<Appeal> GetAppealsByUser(string userToken, int since, int count);
        ICollection<Appeal> GetAppealsByAdmin(int since, int count);
        void UpdateAppealToClosed(long appealId);
        void UpdateAppealToAnswered(long appealId);
        void UpdateAppealToRead(long appealId);
    }
    public class AppealManager
    {
        private IAppealRepository AppealRepository;
        private IUserRepository UserRepository;
        private IAppealMessageRepository AppealMessageRepository;
        private ICategoryRepository CategoryRepository;
        private ILogger Logger;

        public AppealManager(ILogger logger,
            IAppealRepository appealRepository,
            IUserRepository userRepository,
            IAppealMessageRepository appealMessageRepository,
            ICategoryRepository categoryRepository)
        {
            Logger = logger;
            AppealRepository = appealRepository;
            UserRepository = userRepository;
            AppealMessageRepository = appealMessageRepository;
            CategoryRepository = categoryRepository;
        }
        public Appeal Create(CreateAppealCommand command)
        {
            var user = UserRepository.GetByUserTokenNotDeleted(command.UserToken);
            if (user == null)
            {
                throw new NotFoundException("Користувач не був визначений по токену.");
            }
            var appeal = new Appeal
            {
                UserId = user.Id,
                Subject = HttpUtility.UrlDecode(command.Subject),
                State = 1,
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };
            AppealRepository.Create(appeal);
            Logger.Information($"Було створенно нова заява, id={appeal.Id}.");
            return appeal;
        }
        public ICollection<Appeal> GetAppealsByUser(string userToken, int since, int count)
        {
            Logger.Information($"Отримано список користувачем, з={since} по={count}.");
            return AppealRepository.GetAppealsBy(userToken, since, count);
        }
        public ICollection<Appeal> GetAppealsByAdmin(int since, int count)
        {
            Logger.Information($"Отримано список адміном, з={since} по={count}.");
            return AppealRepository.GetAppealsBy(since, count);
        }
        public void UpdateAppealToClosed(long appealId)
        {
            var appeal = AppealRepository.GetBy(appealId);
            if (appeal == null)
            {
                throw new NotFoundException("Звернення не було визначенно сервером по id.");
            }
            appeal.State = 4;
            AppealRepository.Update(appeal);
            Logger.Information($"Звернення було закрите, id={appeal.Id}.");
        }
        public void UpdateAppealToAnswered(long appealId)
        {
            var appeal = AppealRepository.GetBy(appealId);
            if (appeal == null)
            {
                throw new NotFoundException("Звернення не було визначенно сервером по id.");
            }
            if (appeal.State == 1 || appeal.State == 2)
            {
                appeal.State = 3;
                AppealRepository.Update(appeal);
                Logger.Information($"На звернення відповіли. Звернення було оновлено, id={appeal.Id}.");
            }
        }
        public void UpdateAppealToRead(int appealId)
        {
            var appeal = AppealRepository.GetBy(appealId);
            if (appeal == null)
            {
                throw new NotFoundException("Звернення не було визначенно сервером по id.");
            }
            if (appeal.State == 1)
            {
                appeal.State = 2;
                AppealRepository.Update(appeal);
                Logger.Information($"Звернення було оновлено, як прочитане, id={appeal.Id}.");
            }
        }
    }
}