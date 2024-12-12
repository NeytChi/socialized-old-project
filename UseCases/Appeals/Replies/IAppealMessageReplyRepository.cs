using Domain.Admins;

namespace UseCases.Appeals.Replies
{
    public interface IAppealMessageReplyRepository
    {
        void Create(AppealMessageReply reply);
        void Update(AppealMessageReply reply);
        AppealMessageReply Get(long id);
    }
}
