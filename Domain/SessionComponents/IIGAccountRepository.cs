
namespace Domain.SessionComponents
{
    public interface IIGAccountRepository
    {
        IGAccount GetBy(long sessionId);
    }
}
