using Models.Common;
using Models.Statistics;
using Models.AutoPosting;
using Models.GettingSubscribes;

using System.Collections.Generic;

namespace Models.SessionComponents
{
    public partial class IGAccount
    {
        public IGAccount()
        {
            this.AutoPosts = new HashSet<AutoPost>();
            this.Categories = new HashSet<Category>();
            this.Tasks = new HashSet<TaskGS>();
        }
        public long accountId { get; set; }
        public int userId { get; set; }
        public int createdAt { get; set; }
        public string accountUsername { get; set; }
        public string sessionSave { get; set; }
        public bool accountDeleted { get; set; }
        // public virtual BusinessAccount Business { get; set; }
        public virtual AccountProfile Profile { get; set; }
        public virtual SessionState State { get; set; }
        public virtual User User { get; set; }
        public virtual TimeAction timeAction { get; set; }
        public virtual ICollection<TaskGS> Tasks { get; set; }
        public virtual ICollection<AutoPost> AutoPosts { get; set; }
        public virtual ICollection<Category> Categories { get; set; }
    }
}
