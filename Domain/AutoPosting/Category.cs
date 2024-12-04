using Domain.InstagramAccounts;

namespace Domain.AutoPosting
{
    public partial class Category : BaseEntity
    {
        public Category()
        {
            Links = new HashSet<AutoPost>();
        }
        public long AccountId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public bool Deleted { get; set; }
        public virtual IGAccount account { get; set; }
        public virtual ICollection<AutoPost> Links { get; set; }
    }
}