using Domain.Admins;

namespace Infrastructure
{
    public class EmailFollowerRepository
    {
        private Context _context;

        public EmailFollowerRepository(Context context) 
        {
            _context = context;
        }
        public Follower GetByEmail(string email)
        {
            return _context.Followers.Where(f => f.followerEmail == email).FirstOrDefault();
        }
    }
}
