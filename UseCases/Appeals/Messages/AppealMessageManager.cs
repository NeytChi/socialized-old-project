using Serilog;
using System.Web;
using Domain.Admins;
using UseCases.Exceptions;
using UseCases.Appeals.Messages.Commands;

namespace UseCases.Appeals.Messages
{
    public interface IAppealMessageManager
    {
        AppealMessage Create(CreateAppealMessageCommand command);
        void Update(UpdateAppealMessageCommand command);
        void Delete(DeleteAppealMessageCommand command);
    }
    public class AppealMessageManager : BaseManager, IAppealMessageManager
    {
        private IAppealRepository AppealRepository;
        private IAppealMessageRepository AppealMessageRepository;
        private IAppealFileManager AppealFileManager;

        public AppealMessageManager(ILogger logger,
            IAppealRepository appealRepository,
            IAppealMessageRepository appealMessageRepository,
            IAppealFileManager appealFileManager) : base(logger)
        {
            AppealRepository = appealRepository;
            AppealMessageRepository = appealMessageRepository;
            AppealFileManager = appealFileManager;
        }
        public AppealMessage Create(CreateAppealMessageCommand command)
        {
            var appeal = AppealRepository.GetBy(command.AppealId, command.UserToken);
            if (appeal == null)
            {
                throw new NotFoundException("Звернення не було визначенно сервером по id.");
            }
            var message = new AppealMessage()
            {
                AppealId = appeal.Id,
                Message = string.IsNullOrEmpty(command.Message) ? "" : HttpUtility.UrlDecode(command.Message),
                CreatedAt = DateTime.UtcNow,
            };
            AppealMessageRepository.Create(message);
            Logger.Information($"Створено було повідомлення в зверненні, id={message.Id}.");
            message.Files = AppealFileManager.Create(command.Files, message.Id);
            return message;
        }
        public void Update(UpdateAppealMessageCommand command)
        {
            var message = AppealMessageRepository.GetBy(command.MessageId);
            if (message == null)
            {
                throw new NotFoundException("Повідомлення не було визначенно сервером по id.");
            }
            message.Message = command.Message;
            AppealMessageRepository.Update(message);
            Logger.Information($"Повідомлення було оновленно, id={message.Id}..");
        }
        public void Delete(DeleteAppealMessageCommand command)
        {
            var message = AppealMessageRepository.GetBy(command.MessageId);
            if (message == null)
            {
                throw new NotFoundException("Повідомлення не було визначенно сервером по id.");
            }
            message.IsDeleted = true;
            AppealMessageRepository.Update(message);
            Logger.Information($"Повідомлення було видаленно, id={message.Id}.");
        }
    }
}
