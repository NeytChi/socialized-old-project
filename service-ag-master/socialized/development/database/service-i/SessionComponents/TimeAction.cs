using System;

namespace Models.SessionComponents
{
    ///<summary>
    /// Time of session's actions. 
    ///<summary>
    public partial class TimeAction
    {
        public long timeId { get; set; }
        public long accountId { get; set; }
        public bool accountOld { get; set; }
        public int followCount  { get; set; } 
	    public int unfollowCount  { get; set; }
        public int likeCount  { get; set; } 
        public int commentCount  { get; set; } 
        public int mentionsCount  { get; set; } 
        public int blockCount  { get; set; } 
        public int publicationCount  { get; set; } 
        public int messageDirectCount  { get; set; } 
        public int watchingStoriesCount  { get; set; } 
        public DateTime followLastAt { get; set; }
        public DateTime unfollowLastAt { get; set; }
        public DateTime likeLastAt { get; set; }
        public DateTime commentLastAt { get; set; }
        public DateTime mentionsLastAt { get; set; }
        public DateTime blockLastAt { get; set; }
        public DateTime publicationLastAt { get; set; }
        public DateTime messageDirectLastAt { get; set; }
        public DateTime watchingStoriesLastAt { get; set; }
        public virtual IGAccount account { get; set; }   
    }
}