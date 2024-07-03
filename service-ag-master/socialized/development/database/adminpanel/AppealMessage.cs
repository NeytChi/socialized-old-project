using System;
using System.Collections.Generic;

namespace Models.AdminPanel
{
    public partial class AppealMessage
    {
        public AppealMessage()
        {
            files = new HashSet<AppealFile>();
        }
        public long messageId { get; set; }
        public int appealId { get; set; }
        public int? adminId { get; set; }
        public string messageText { get; set; }
        public DateTimeOffset createdAt { get; set; }
        public DateTimeOffset updatedAt { get; set; }
        public virtual Appeal appeal { get; set; }
        public virtual Admin admin { get; set; }
        public virtual ICollection<AppealFile> files { get; set; }
    }
}