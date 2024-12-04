
namespace Domain.InstagramAccounts
{
    public interface IIGAccountRepository
    {
        IGAccount GetBy(long accountId);
    }
}
