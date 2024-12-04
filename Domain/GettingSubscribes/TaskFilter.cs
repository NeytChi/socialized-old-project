namespace Domain.GettingSubscribes
{
    public partial class TaskFilter : BaseEntity
    {
        public TaskFilter()
        {
            words = new HashSet<FilterWord>();
        }
        public long TaskId { get; set; }
        public int RangeSubscribersFrom { get; set; }
        public int RangeSubscribersTo { get; set; }
        public int RangeFollowingFrom { get; set; }
        public int RangeFollowingTo { get; set; }
        public int PublicationCount { get; set; }
        public int LatestPublicationNoYounger { get; set; }
        public bool WithoutProfilePhoto { get; set; }
        public bool WithProfileUrl { get; set; }
        public bool English { get; set; }
        public bool Ukrainian { get; set; }
        public bool Russian { get; set; }
        public bool Arabian { get; set; }
        public virtual TaskGS Task { get; set; }
        public ICollection<FilterWord> words { get; set; }
    };
}
