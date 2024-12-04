namespace Domain.AutoPosting
{
    public class GetAutoPostsCommand
    {
        public string UserToken { get; set; }
        public long SessionId { get; set; }
        public bool PostExecuted { get; set; }
        public bool PostDeleted { get; set; }
        public bool PostAutoDeleted { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int Since { get; set; }
        public int Count { get; set; }
    }
}
