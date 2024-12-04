namespace Domain.InstagramAccounts
{
    public partial class TimeAction : BaseEntity
    {
        public long AccountId { get; set; }
        public bool AccountOld { get; set; }
        public int FollowCount  { get; set; } 
	    public int UnfollowCount  { get; set; }
        public int LikeCount  { get; set; } 
        public int CommentCount  { get; set; } 
        public int MentionsCount  { get; set; } 
        public int BlockCount  { get; set; } 
        public int PublicationCount  { get; set; } 
        public int MessageDirectCount  { get; set; } 
        public int WatchingStoriesCount  { get; set; } 
        public DateTime FollowLastAt { get; set; }
        public DateTime UnfollowLastAt { get; set; }
        public DateTime LikeLastAt { get; set; }
        public DateTime CommentLastAt { get; set; }
        public DateTime MentionsLastAt { get; set; }
        public DateTime BlockLastAt { get; set; }
        public DateTime PublicationLastAt { get; set; }
        public DateTime MessageDirectLastAt { get; set; }
        public DateTime WatchingStoriesLastAt { get; set; }
        public virtual IGAccount Account { get; set; }   
    }
}