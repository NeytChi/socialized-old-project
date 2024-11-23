namespace Domain.AutoPosting
{
    public partial class PostFile
    {
        public long fileId { get; set; }
        public long postId { get; set; }
        public string filePath { get; set; }
        public bool fileDeleted { get; set; }
        public sbyte fileOrder { get; set; }
        public bool fileType { get; set; }
        public string mediaId { get; set; }
        public string videoThumbnail { get; set; }
        public DateTimeOffset createdAt { get; set; }
        public virtual AutoPost post { get; set; }
    }
}