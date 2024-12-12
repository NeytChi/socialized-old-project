using Domain.Admins;

namespace UseCases.Appeals
{
    public interface IAppealMessageRepository
    {
        AppealMessage Create(AppealMessage message);
        void Update(AppealMessage message);
        AppealMessage GetBy(long messageId);
    }
}
