using System;

namespace Models.Statistics
{
    public partial class CommentStatistics
    {
        public long commentId { get; set; }
        public long mediaId { get; set; }
        public string commentIGId { get; set; }
        public DateTime timestamp { get; set; }
        public virtual PostStatistics Post { get; set; }       
    }
}