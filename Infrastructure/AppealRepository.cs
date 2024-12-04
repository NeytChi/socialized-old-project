using Domain.Admins;
using Domain.Users;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return _context.Appeals.Where(a => a.appealId == appealId).FirstOrDefault();
        }
        public Appeal GetBy(int appealId, string userToken)
        {
            return (from a in _context.Appeals
                    join u in _context.Users on a.userId equals u.userId
                    where a.appealId == appealId
                        && a.appealState != 4
                        && u.userToken == userToken
                    select a).FirstOrDefault();
        }
        public Appeal[] GetAppealsBy(string userToken, int since = 0, int count = 10)
        {
            return (from appeal in _context.Appeals
                    join user in _context.Users on appeal.userId equals user.userId
                    where user.userToken == userToken
                    && user.deleted == false
                    orderby appeal.appealState
                    orderby appeal.createdAt descending
                    select appeal).Skip(since * count).Take(count).ToArray();
        }
        public Appeal[] GetAppealsBy(int since, int count)
        {
            return (from appeal in _context.Appeals
                    orderby appeal.appealState
                    orderby appeal.createdAt descending
                    select appeal)
            .Skip(since * count).Take(count).ToArray();
        }
    }
}
