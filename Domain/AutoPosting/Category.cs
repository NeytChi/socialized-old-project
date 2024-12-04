using Domain.SessionComponents;

namespace Domain.AutoPosting
{
    public partial class Category
    {
        public Category()
        {
            this.links = new HashSet<AutoPost>();
        }
        public long categoryId { get; set; }
        public long accountId { get; set; }
        public string categoryName { get; set; }
        public string categoryColor { get; set; }
        public bool categoryDeleted { get; set; }
        public DateTimeOffset createdAt { get; set; }
        public virtual IGAccount account { get; set; }
        public virtual ICollection<AutoPost> links { get; set; }
    }
    public struct CategoryCache
    {
        public string user_token { get; set; }
        public long category_id { get; set; }
        public long account_id { get; set; }
        public string category_name { get; set; }
        public string category_color { get; set; }
    }
}