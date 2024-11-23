using System;
using System.Drawing;
using System.Runtime.Remoting.Contexts;
using System.Security.Policy;
using Domain.Admins;

namespace Infrastructure
{
    public class AdminRepository
    {
        private Context _context;
        public AdminRepository(Context context) 
        {
            _context = context;
        }

        public void Create(Admin admin)
        {
            _context.Admins.Add(admin);
            _context.SaveChanges();
        }
        public void Update(Admin admin)
        {
            _context.Admins.Update(admin);
            _context.SaveChanges();
        }
        public void Delete(Admin admin)
        {

        }
        public Admin GetByAdminId(int id)
        {
            return _context.Admins.Where(a => a.adminId == id && a.deleted == false).FirstOrDefault();
        }
        public Admin GetByEmail(string email)
        {
            return _context.Admins.Where(a => a.adminEmail == email && a.deleted == false).FirstOrDefault();
        }
        public Admin GetByPasswordToken(string passwordToken)
        {
            return _context.Admins.Where(a => a.passwordToken == passwordToken && a.deleted == false).FirstOrDefault();
        }
        public Admin GetByRecoveryCode(int recoveryCode)
        {
            return _context.Admins.Where(a => a.recoveryCode == recoveryCode).FirstOrDefault();
        }
        public Admin[] GetActiveAdmins(int adminId, int since, int count)
        {
            return _context.Admins.Where(a => a.adminId != adminId && a.deleted == false)
                .Skip(since * count)
                .Take(count)
                .ToArray();
        }
        public dynamic GetFollowers(int since, int count)
        {
            return _context.Followers.GroupJoin(
                _context.Users,
                follower => follower.userId,
                user => user.userId,
                (follower, users) => new { follower, users })
                    .OrderByDescending(fu => fu.follower.followerId)
                    .Select(fu => new
                    {
                        follower_id = fu.follower.followerId,
                        follower_email = fu.follower.followerEmail,
                        created_at = fu.follower.createdAt,
                        enable_mailing = fu.follower.enableMailing,
                        has_user = fu.follower.userId != 0
                            && fu.users.All(u =>
                                u.deleted == false &&
                                u.activate == true)
                    })
                    .Skip(since * count)
                    .Take(count);
        }
        public dynamic GetUsers(int since, int count)
        {
            return (from user in _context.Users
             join profile in _context.UserProfile on user.userId equals profile.userId
             join follower in _context.Followers on user.userId equals follower.userId into followers
             where user.deleted == false
               && user.activate == true
             orderby user.userId descending
             select new
             {
                 user_email = user.userEmail,
                 country = profile.country,
                 registration = user.createdAt,
                 activity = user.lastLoginAt,
                 sessions_count = (from businessAccount in _context.BusinessAccounts
                                   where businessAccount.userId == user.userId
                                       && !businessAccount.deleted
                                   select businessAccount).Count(),
                 subscription = followers.Count() == 1 ? true : false
             })
            .Skip(since * count)
            .Take(count)
            .ToArray();
        }
    }
}
