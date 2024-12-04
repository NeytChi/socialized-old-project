using Domain.InstagramAccounts;

namespace Domain.AutoPosting
{
    public partial class AutoPost : BaseEntity
    {
        public AutoPost()
        {
            files = new HashSet<AutoPostFile>();
        }
        public long AccountId { get; set; }
        public bool Type { get; set; }
        public bool Executed { get; set; }
        public bool Deleted { get; set; }
        public bool Stopped { get; set; }
        public bool AutoDelete { get; set; }
        public bool AutoDeleted { get; set; }
        public DateTime ExecuteAt { get; set; }
        public DateTime DeleteAfter { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public int TimeZone { get; set; }
        public long CategoryId { get; set; }
        public virtual Category category { get; set; }
        public virtual IGAccount account { get; set; }
        public virtual ICollection<AutoPostFile> files { get; set; }
    }
}