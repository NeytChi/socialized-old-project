using Domain.Admins;

namespace Infrastructure
{
    public class AppealRepository
    {
        private Context _context;
        public AppealRepository(Context context) 
        {
            _context = context;
        }
        public Appeal GetBy(int appealId)
        {
            return _context.Appeals.Where(a => a.Id == appealId).FirstOrDefault();
        }
        public Appeal GetBy(long appealId, string userToken, int appealStateIsNot = 4)
        {
            return (from a in _context.Appeals
                    join u in _context.Users on a.UserId equals u.Id
                    where a.Id == appealId
                        && a.State != appealStateIsNot
                        && u.TokenForUse == userToken
                    select a).FirstOrDefault();
        }
        public Appeal[] GetAppealsBy(string userToken, int since = 0, int count = 10, bool IsUserDeleted = false)
        {
            return (from appeal in _context.Appeals
                    join user in _context.Users on appeal.UserId equals user.Id
                    where user.TokenForUse == userToken
                    && user.IsDeleted == IsUserDeleted
                    orderby appeal.State
                    orderby appeal.CreatedAt descending
                    select appeal).Skip(since * count).Take(count).ToArray();
        }
        public Appeal[] GetAppealsBy(int since, int count)
        {
            return (from appeal in _context.Appeals
                    orderby appeal.State
                    orderby appeal.CreatedAt descending
                    select appeal)
            .Skip(since * count).Take(count).ToArray();
        }
    }
}
