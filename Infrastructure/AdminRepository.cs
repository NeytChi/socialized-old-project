using Domain;

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
        public Admin GetByAdminId(long adminId, bool deleted = false)
        {
            return _context.Admins.Where(a => a.Id == adminId && a.IsDeleted == deleted).FirstOrDefault();
        }
        public Admin GetByEmail(string email, bool deleted = false)
        {
            return _context.Admins.Where(a => a.Email == email && a.IsDeleted == deleted).FirstOrDefault();
        }
        public Admin GetByPasswordToken(string passwordToken, bool deleted = false)
        {
            return _context.Admins.Where(a => a.TokenForStart == passwordToken && a.IsDeleted == deleted).FirstOrDefault();
        }
        public Admin GetByRecoveryCode(int recoveryCode)
        {
            return _context.Admins.Where(a => a.RecoveryCode == recoveryCode).FirstOrDefault();
        }
        public Admin[] GetActiveAdmins(int adminId, int since, int count, bool isDeleted = false)
        {
            return _context.Admins.Where(a => a.Id != adminId && a.IsDeleted == isDeleted)
                .Skip(since * count)
                .Take(count)
                .ToArray();
        }
        public dynamic GetUsers(int since, int count, bool isDeleted = false, bool activate = true)
        {
            return (from user in _context.Users
             join profile in _context.UserProfile on user.Id equals profile.UserId
             where user.IsDeleted == isDeleted
               && user.Activate == activate
             orderby user.Id descending
             select new
             {
                 user_email = user.Email,
                 registration = user.CreatedAt,
                 activity = user.LastLoginAt,
                 sessions_count = (from businessAccount in _context.BusinessAccounts
                    where businessAccount.AccountId == user.Id && !businessAccount.IsDeleted
                    select businessAccount).Count(),
             })
            .Skip(since * count)
            .Take(count)
            .ToArray();
        }
    }
}
