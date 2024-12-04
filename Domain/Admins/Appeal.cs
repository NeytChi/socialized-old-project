using Domain.Users;

namespace Domain.Admins
{
    public partial class Appeal : BaseEntity
    {
        public Appeal()
        {
            Messages = new HashSet<AppealMessage>();
        }
        public int UserId { get; set; }
        public string Subject { get; set; }
        public int State { get; set; }
        public DateTimeOffset LastActivity { get; set; }
        public virtual User User { get; set; }
        public ICollection<AppealMessage> Messages { get; set; }
    }
}
