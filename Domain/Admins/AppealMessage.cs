namespace Domain.Admins
{
    public partial class AppealMessage : BaseEntity
    {
        public AppealMessage()
        {
            Files = new HashSet<AppealFile>();
        }
        public long AppealId { get; set; }
        public long AdminId { get; set; }
        public string Value { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public virtual Appeal Appeal { get; set; }
        public virtual Admin Admin { get; set; }
        public virtual ICollection<AppealFile> Files { get; set; }
    }
}