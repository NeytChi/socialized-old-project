using Domain.SessionComponents;

namespace Domain.AutoPosting
{
    public partial class AutoPost
    {
        public AutoPost()
        {
            this.files = new HashSet<PostFile>();
        }
        public long postId { get; set; }
        public long sessionId { get; set; }
        public bool postType { get; set; }
        public bool postExecuted { get; set; }
        public bool postDeleted { get; set; }
        public bool postStopped { get; set; }
        public bool postAutoDeleted { get; set; }
        public DateTimeOffset createdAt { get; set; }
        public DateTime executeAt { get; set; }
        public bool autoDelete { get; set; }
        public DateTime deleteAfter { get; set; }
        public string postLocation { get; set; }
        public string postDescription { get; set; }
        public string postComment { get; set; }
        public int timezone { get; set; }
        public long categoryId { get; set; }
        public virtual Category category { get; set; }
        public virtual IGAccount account { get; set; }
        public virtual ICollection<PostFile> files { get; set; }
    }
}