using Newtonsoft.Json.Linq;

namespace InstagramService.Statistics
{
    public struct StatisticsObject
    {
        public string name;
        public string period;
        public List<StatisticsValue> values;
    }
    public struct StatisticsValue
    {
        public long value;
        public DateTime end_time;
    }
    public struct StatisticsOnlineFollowers
    {
        public string name;
        public string period;
        public List<OnlineFollowersValues> values;
    }
    public struct OnlineFollowersValues
    {
        public JObject value;
        public DateTime end_time;
    }
    public struct PostValues
    {
        public JObject comments;
        public long like_count;
        public string media_url;
        public int comments_count;
        public string id;
        public string media_type;
        public DateTime timestamp;
    }
    public struct StoryValues
    {
        public string media_url;
        public string id;
        public string media_type;
        public DateTime timestamp;
    }
    public struct CommentValue
    {
        public DateTime timestamp;
        public string id;
    }
}