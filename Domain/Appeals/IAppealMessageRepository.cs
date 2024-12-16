using Domain.Admins;

namespace Domain.Appeals
{
    public interface IAppealMessageRepository
    {
        AppealMessage Create(AppealMessage message);
        void Update(AppealMessage message);
        AppealMessage GetBy(long messageId);
    }
}
