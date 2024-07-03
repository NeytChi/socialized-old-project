using Models.Common;

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Models.AdminPanel
{
    public partial class Appeal
    {
        public Appeal()
        {
            this.messages = new HashSet<AppealMessage>();
        }
        public int appealId { get; set; }
        public int userId { get; set; }
        public string appealSubject { get; set; }
        public int appealState { get; set; }
        public DateTimeOffset createdAt { get; set; }
        public DateTimeOffset lastActivity { get; set; }
        public virtual User user { get; set; }
        public ICollection<AppealMessage> messages { get; set; }
    }
    public enum AppealState
    {
        New = 1,
        Read = 2,
        Answered = 3,
        Completed = 4
    }
    public struct SupportCache
    {
        public string user_token;
        public string appeal_subject;
        public string appeal_message;
        public List<IFormFile> files;
        public int since;
        public int count;
        public int appeal_id;
        public int admin_id;
    }
}
