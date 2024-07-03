using System;

namespace Models.SessionComponents
{
    public partial class AccountProfile
    {
        public long profileId { get; set; }
        public long accountId { get; set; }
        public DateTime updatedAt { get; set; }
        public string username { get; set; }
        public long postsCount { get; set; }
        public long followingCount { get; set; }
        public long subscribersCount { get; set; }
        public string avatarUrl { get; set; }
        public long subscribersGS { get; set; }
        public long subscribersTodayGS { get; set; }
        public long conversionGS { get; set; }
        public virtual IGAccount account { get; set; }
    }
}