namespace Domain.GettingSubscribes
{
    ///<summary>
    /// Task of "Getting Subscribers"
    ///<summary>
    public partial class TaskData
    {
        public TaskData()
        {
            Units = new HashSet<UnitGS>();
        }
        public long dataId { get; set; }
        public long taskId { get; set; }
        public string dataNames { get; set; }
        public double? dataLongitute { get; set; }
        public double? dataLatitute { get; set; }
        public string dataComment { get; set; }
        public bool dataDeleted { get; set; }
        public bool dataStopped { get; set; }
        public int nextPage { get; set; }
        public virtual TaskGS Task { get; set; }
        public ICollection<UnitGS> Units { get; set; }
    }
}
