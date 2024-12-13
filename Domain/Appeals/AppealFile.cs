namespace Domain.Admins
{
    public partial class AppealFile : BaseEntity
    {
        public long MessageId { get; set; }
        public string RelativePath { get; set; }
        public virtual AppealMessage Message { get; set; }
    }
}