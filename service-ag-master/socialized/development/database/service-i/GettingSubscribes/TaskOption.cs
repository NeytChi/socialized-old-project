namespace Models.GettingSubscribes
{
    ///<summary>
    /// Task of "Getting Subscribers"
    ///<summary>
    public partial class TaskOption
    {
        public TaskOption()
        {
            
        }
        public long optionId { get; set; }
        public long taskId { get; set; }
        public bool dontFollowOnPrivate { get; set; }
        public bool watchStories { get; set; }
        public bool likeUsersPost { get; set; }
        public bool autoUnfollow { get; set; }
        public bool unfollowNonReciprocal { get; set; }
        public bool nextUnlocking { get; set; }
        public int likesOnUser { get; set; }
        public virtual TaskGS Task { get; set; }
        
    }
    public struct OptionCache
    {
        public bool dont_follow_on_private { get; set; }
        public bool watch_stories { get; set; }
        public bool like_users_post { get; set; }
        public bool auto_unfollow { get; set; }
        public bool unfollow_only_from_non_reciprocal { get; set; }
        public bool next_unlocking { get; set; }
        public int likes_on_user { get; set; }
        
    }
}
