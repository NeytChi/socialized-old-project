using Domain.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class UserRepository
    {
        private Context _context;
        public UserRepository(Context context)
        {
            _context = context;
        }
        public User GetByEmail(string email)
        {
            return _context.Users.Where(u => u.userEmail == email).FirstOrDefault();
        }
        public User GetByEmail(string email, bool deleted)
        {
            return _context.Users.Where(u => u.userEmail == email && u.deleted == deleted).FirstOrDefault();
        }
        public User GetByEmail(string email, bool deleted, bool activate)
        {
            return _context.Users.Where(u => u.userEmail == email
                && u.deleted == false
                && u.activate == true).FirstOrDefault();
        }
        public User GetByUserTokenNotDeleted(string userToken)
        {
            return _context.Users.Where(u => u.userToken == userToken && u.deleted == false).FirstOrDefault();
        }
        public User GetByRecoveryToken(string recoveryToken, bool deleted)
        {
            return _context.Users.Where(u => u.recoveryToken == recoveryToken && u.deleted == deleted).FirstOrDefault();
        }
        public User GetByHash(string hash, bool deleted, bool activate)
        {
            return _context.Users.Where(u => u.userHash == hash && u.activate == activate && u.deleted == deleted).FirstOrDefault();
        }
    }
}
