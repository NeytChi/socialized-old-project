namespace Domain.GettingSubscribes
{
    public partial class TaskOption : BaseEntity
    {
        public long TaskId { get; set; }
        public bool DontFollowOnPrivate { get; set; }
        public bool WatchStories { get; set; }
        public bool LikeUsersPost { get; set; }
        public bool AutoUnfollow { get; set; }
        public bool UnfollowNonReciprocal { get; set; }
        public bool NextUnlocking { get; set; }
        public int LikesOnUser { get; set; }
        public virtual TaskGS Task { get; set; }
    }
}
