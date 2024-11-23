namespace Domain.GettingSubscribes
{
    public partial class TaskFilter
    {
        public TaskFilter()
        {
            words = new HashSet<FilterWord>();
        }
        public long filterId { get; set; }
        public long taskId { get; set; }
        public int range_subscribers_from { get; set; }
        public int range_subscribers_to { get; set; }
        public int range_following_from { get; set; }
        public int range_following_to { get; set; }
        public int publication_count { get; set; }
        public int latest_publication_no_younger { get; set; }
        public bool without_profile_photo { get; set; }
        public bool with_profile_url { get; set; }
        public bool english { get; set; }
        public bool ukrainian { get; set; }
        public bool russian { get; set; }
        public bool arabian { get; set; }
        public ICollection<FilterWord> words { get; set; }
        public List<string> words_in_description;
        public List<string> no_words_in_description;
        public virtual TaskGS Task { get; set; }
    };
}
