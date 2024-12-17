using Serilog;
using Domain.Admins;
using UseCases.Exceptions;
using UseCases.Appeals.Replies.Commands;
using Domain.Appeals;
using Domain.Appeals.Replies;

namespace UseCases.Appeals.Replies
{
    public interface IAppealMessageReplyManager
    {
        AppealMessageReply Create(CreateAppealMessageReplyCommand command);
        void Update(UpdateAppealMessageReplyCommand command);
        void Delete(DeleteAppealMessageReplyCommand command);
    }
    public class AppealMessageReplyManager : BaseManager, IAppealMessageReplyManager
    {
        private IAppealManager AppealManager;
        private IAppealMessageRepository MessageRepository;
        private IAppealMessageReplyRepository ReplyRepository;

        public AppealMessageReplyManager(ILogger logger,
            IAppealMessageReplyRepository appealMessageReplyRepository,
            IAppealMessageRepository messageRepository,
            IAppealManager appealManager) : base(logger)
        {
            ReplyRepository = appealMessageReplyRepository;
            MessageRepository = messageRepository;
            AppealManager = appealManager;
        }
        public AppealMessageReply Create(CreateAppealMessageReplyCommand command)
        {
            var message = MessageRepository.GetBy(command.AppealMessageId);
            if (message == null)
            {
                throw new NotFoundException("Повідомлення не було визначенно сервером по id.");
            }
            AppealManager.UpdateAppealToAnswered(message.AppealId);

            var reply = new AppealMessageReply
            {
                AppealMessageId = message.Id,
                Reply = command.Reply,
                Message = message,
                CreatedAt = DateTime.UtcNow,
            };
            ReplyRepository.Create(reply);
            Logger.Information($"Було створенно відповідь на повідомлення, id={reply.Id}.");
            return reply;
        }
        public void Update(UpdateAppealMessageReplyCommand command)
        {
            var reply = ReplyRepository.Get(command.ReplyId);
            if (reply == null)
            {
                throw new NotFoundException("Відповідь не була знайдена по id.");
            }
            reply.Reply = command.Reply;
            ReplyRepository.Update(reply);
            Logger.Information($"Відповідь була оновленна, id={reply.Id}.");
        }
        public void Delete(DeleteAppealMessageReplyCommand command)
        {
            var reply = ReplyRepository.Get(command.ReplyId);
            if (reply == null)
            {
                throw new NotFoundException("Відповідь не була знайдена по id.");
            }
            reply.IsDeleted = true;
            ReplyRepository.Update(reply);
            Logger.Information($"Відповідь була видаленна, id={reply.Id}.");
        }
    }
}
