using Domain.GettingSubscribes;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class TaskDataRepository
    {
        private Context _context;

        public TaskDataRepository(Context context)
        {
            _context = context;
        }
        public TaskData GetBy(string userToken, long dataId, bool deleted = false)
        {
            return (from d in _context.TaskData
                 join t in _context.TaskGS on d.taskId equals t.taskId
                 join s in _context.IGAccounts on t.sessionId equals s.accountId
                 join u in _context.Users on s.userId equals u.userId
                 where u.userToken == userToken && d.dataId == dataId && d.dataDeleted == deleted
                 select d)
                 .FirstOrDefault();
        }
        public List<TaskData> GetBy(long taskId, bool deleted = false)
        {
            return _context.TaskData.Where(d => d.dataDeleted == deleted && d.taskId == taskId).ToList();
        }
    }
}
