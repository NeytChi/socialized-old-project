using System;

namespace Models.Lending
{
    public partial class Follower
    {
        public long followerId { get; set; }
        public int userId { get; set; }
        public string followerEmail { get; set; }
        public DateTime createdAt { get; set; }
        public bool enableMailing { get; set; }
    }
    public struct FollowerCache
    {
        public string follower_email;
        public long follower_id;
    }
}