using Domain.GettingSubscribes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.Users
{
    public class UserRemoverManager
    {
        public void DeleteUsersData(int userId)
        {
            var accounts = context.IGAccounts.Where(s => s.userId == userId).ToArray();
            foreach (var account in accounts)
            {
                account.accountDeleted = true;
                var tasks = context.TaskGS.Where(t => t.sessionId == account.accountId).ToArray();
                foreach (TaskGS task in tasks)
                {
                    task.taskStopped = true;
                    task.taskDeleted = true;
                }
                context.UpdateRange(tasks);
                Logger.Information("Delete instagram account, id ->" + userId);
            }
            context.IGAccounts.UpdateRange(accounts);
            context.SaveChanges();
        }
    }
}
