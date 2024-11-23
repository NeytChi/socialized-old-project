namespace Domain.Admins
{
    public partial class AppealFile
    {
        public AppealFile()
        {

        }
        public long fileId { get; set; }
        public long messageId { get; set; }
        public string relativePath { get; set; }
        public virtual AppealMessage message { get; set; }
    }
}