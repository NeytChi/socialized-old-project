using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

using Models.SessionComponents;

namespace Models.AutoPosting
{
    public partial class AutoPost
    {
        public AutoPost()
        {
            this.files = new HashSet<PostFile>();
        }
        public long postId { get; set; }
        public long sessionId { get; set; }
        public bool postType { get; set; }
        public bool postExecuted { get; set; }
        public bool postDeleted { get; set; }
        public bool postStopped { get; set; }
        public bool postAutoDeleted { get; set; }
        public DateTimeOffset createdAt { get; set; }
        public DateTime executeAt { get; set; }
        public bool autoDelete { get; set; }
        public DateTime deleteAfter { get; set; }
        public string postLocation { get; set; }
        public string postDescription { get; set; }
        public string postComment { get; set; }
        public int timezone { get; set; }
        public long categoryId { get; set; }
        public virtual Category category { get; set; }
        public virtual IGAccount account { get; set; }
        public virtual ICollection<PostFile> files { get; set; }
    }
    public struct AutoPostCache
    {
        public string user_token;
        public long session_id;
        public long post_id;
        public bool post_type;
        public List<IFormFile> files;
        public List<long> files_id;
        public DateTime execute_at;
        public bool? auto_delete;
        public DateTime delete_after;
        public string location;
        public string comment;
        public string description;
        public int category;
        public long category_id;
        public int next;
        public int count;
        public DateTime from;
        public DateTime to;
        public long file_id;
        public sbyte order;
        public int timezone;
    }
}