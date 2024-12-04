namespace Domain.GettingSubscribes
{
    public partial class TaskData : BaseEntity
    {
        public TaskData()
        {
            Units = new HashSet<UnitGS>();
        }
        public long TaskId { get; set; }
        public string Names { get; set; }
        public double? Longitute { get; set; }
        public double? Latitute { get; set; }
        public string Comment { get; set; }
        public bool Stopped { get; set; }
        public int NextPage { get; set; }
        public virtual TaskGS Task { get; set; }
        public ICollection<UnitGS> Units { get; set; }
    }
}
