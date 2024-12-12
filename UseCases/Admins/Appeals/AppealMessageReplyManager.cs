using Serilog;
using Domain.Admins;
using UseCases.Exceptions;

namespace UseCases.Admins.Appeals
{
    public interface IAppealMessageReplyManager
    {
        AppealMessageReply Create(CreateAppealMessageReplyCommand command);
        void Update(UpdateAppealMessageReplyCommand command);
        void Delete(DeleteAppealMessageReplyCommand command);
    }
    public class AppealMessageReplyManager : BaseManager, IAppealMessageReplyManager
    {
        private IAppealRepository AppealRepository;
        private IAppealManager AppealManager;

        public AppealMessageReplyManager(ILogger logger,
            IAppealRepository appealRepository,
            IAppealManager appealManager) : base (logger) 
        {
            AppealRepository = appealRepository;
            AppealManager = appealManager;
        }
        public AppealMessageReply Create(CreateAppealMessageReplyCommand command)
        {
            /*var appeal = AppealRepository.GetBy(command.AppealMessageId);
            if (appeal == null)
            {
                throw new NotFoundException("Звернення не було визначенно сервером по id.");
            }
            AppealManager.UpdateAppealToAnswered(appeal);
            */
            throw new NotImplementedException();
        }
        public void Delete(DeleteAppealMessageReplyCommand command)
        {

            throw new NotImplementedException();
        }
        public void Update(UpdateAppealMessageReplyCommand command)
        {

            throw new NotImplementedException();
        }
    }
}
