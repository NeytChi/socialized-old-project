using System;

namespace Models.AdminPanel
{
    public partial class BlogPost
    {
        public BlogPost()
        {
            
        }
        public int postId { get; set; }
        public int adminId { get; set; }
        public string postSubject { get; set; }
        public string postHtmlText { get; set; }
        public int postLanguage { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public bool deleted { get; set; }
        public virtual Admin admin { get; set; }
    }
    public enum BlogLanguage 
    {
        English = 1,
        Russian = 2
    }
    public struct BlogCache
    {
        public int post_id;
        public int admin_id;
        public string post_subject;
        public string post_htmltext;
        public int post_language;
    }
}