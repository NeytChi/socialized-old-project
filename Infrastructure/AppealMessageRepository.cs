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
        public AppealMessage[] GetAppealMessages(int appealId, int since, int count)
        {
            return (from message in _context.AppealMessages
                    join appeal in _context.Appeals on message.appealId equals appeal.appealId
                    join user in _context.Users on appeal.userId equals user.userId
                    join admin in _context.Admins on message.adminId equals admin.adminId into admins
                    join file in _context.AppealFiles on message.messageId equals file.messageId into files
                    where appeal.appealId == appealId
                    orderby message.messageId descending
                    select message
                    /*{
                        message_id = message.messageId,
                        message_text = message.messageText,
                        created_at = message.createdAt,
                        files = files.Select(f => new
                        {
                            file_id = f.fileId,
                            // file_url = fileDomen + f.relativePath
                        }).ToArray(),
                        // sender = admins.Count() == 1 ? GetSender(admins) : GetSender(user)
                    }*/)
                .Skip(since * count).Take(count).ToArray();
        }
    }
}
