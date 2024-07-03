using System;
using System.Collections.Generic;

using Models.Common;

namespace Models.Statistics
{
    public partial class BusinessAccount
    {
        public long businessId { get; set; }
        public long igAccountId { get; set; }
        public int userId { get; set; }
        public string accessToken { get; set; }
        public string profilePicture { get; set; }
        public string accountUsername { get; set; }
        public string longLiveAccessToken { get; set; }
        public string facebookId { get; set; }
        public string businessAccountId { get; set; }
        public long followersCount { get; set; }
        public int mediaCount { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime longTokenExpiresIn { get; set; }
        public bool deleted { get; set; }
        public DateTime tokenCreated { get; set; }
        public bool received { get; set; }
        public bool startProcess { get; set; }
        public DateTime startedProcess { get; set; }
        public virtual User user { get; set; }
        // public virtual IGAccount igAccount { get; set; }
        public virtual ICollection<DayStatistics> Statistics { get; set; }
        public virtual ICollection<OnlineFollowers> OnlineFollowers { get; set; }
        public virtual ICollection<PostStatistics> Posts { get; set; }
        public virtual ICollection<StoryStatistics> Stories { get; set; }
        
    }
}