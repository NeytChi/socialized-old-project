using Domain.Users;
using Domain.AutoPosting;
using Domain.GettingSubscribes;

namespace Domain.InstagramAccounts
{
    public partial class IGAccount : BaseEntity
    {
        public IGAccount()
        {
            Categories = new HashSet<Category>();
            Tasks = new HashSet<TaskGS>();
        }
        public long UserId { get; set; }
        public string Username { get; set; }
        public virtual BusinessAccount Business { get; set; }
        public virtual AccountProfile Profile { get; set; }
        public virtual SessionState State { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<TaskGS> Tasks { get; set; }
        public virtual ICollection<AutoPost> AutoPosts { get; set; }
        public virtual ICollection<Category> Categories { get; set; }
    }
}
