using Domain.GettingSubscribes;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class TaskGsRepository
    {
        private Context _context;

        public TaskGsRepository(Context context) 
        {
            _context = context;
        }
        public TaskGS GetBy(long taskId, bool taskDeleted = false)
        {
            return (from t in _context.TaskGS where t.taskId == taskId && t.taskDeleted == false
             select t).FirstOrDefault();
        }
        public TaskGS GetBy(string userToken, long taskId, bool taskDeleted = false)
        {
            return (from t in _context.TaskGS
                    join s in _context.IGAccounts on t.sessionId equals s.accountId
                    join u in _context.Users on s.userId equals u.userId
                    where u.userToken == userToken
                        && t.taskId == taskId
                        && t.taskDeleted == taskDeleted
                    select t).FirstOrDefault();
        }
    }
}
