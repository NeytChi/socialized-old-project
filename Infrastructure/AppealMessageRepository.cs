using Domain.Admins;

namespace Infrastructure
{
    public class AppealMessageRepository
    {
        public Context _context;
        public AppealMessageRepository(Context context)
        {
            _context = context;
        }
    }
}
