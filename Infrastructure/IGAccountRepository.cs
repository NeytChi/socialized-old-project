using Domain.SessionComponents;

namespace Infrastructure
{
    public class IGAccountRepository
    {
        private Context Context;
        public IGAccountRepository(Context context)
        {
            Context = context;
        }
        public IGAccount GetBy(long sessionId)
        {
            return Context.IGAccounts.Where(a => a.accountId == sessionId).FirstOrDefault();
        }
    }
}
