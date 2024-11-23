using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Controllers;
using database.context;
using Models.Statistics;

namespace InstagramService.Statistics
{
    public class InsightStatistics : IStatistics
    {
        public InsightStatistics(StatisticsService service, Context context, JsonHandler handler)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
        }
        public JsonHandler handler;
        public StatisticsService service;
        public Context context;
        public void GetStatistics(BusinessAccount account)
        {
            InsightsPost(account);
            InsightsStories(account);
        }
        public void InsightsPost(BusinessAccount account)
        {
            List<StatisticsObject> insights; List<PostStatistics> posts;
            string response, url;

            posts = context.PostStatistics.Where(p => p.accountId == account.businessId).ToList();
            foreach (var post in posts) 
            {
                if ((url = GetURLPostInsights(post.IGMediaId, post.mediaType, account.longLiveAccessToken)) != null) {
                    response = service.GetFacebookRequest(url);
                    if (!string.IsNullOrEmpty(response)) 
                    {
                        var data = handler.handle(JsonConvert.DeserializeObject<JObject>(response), "data", JTokenType.Array);
                        if (data != null) 
                        {
                            insights = data.ToObject<List<StatisticsObject>>();
                            UpdatePostInsights(insights, post);
                        }
                    }
                }
            }
        }
        public string GetURLPostInsights(string IGMediaId, string mediaType, string accessToken)
        {
            if (CheckTokenAndIG(accessToken, IGMediaId)) 
            {
                if (mediaType == "VIDEO")
                    return IGMediaId + "/insights?metric=engagement,impressions,reach,saved,video_views" 
                    + "&access_token=" + accessToken;
                else
                    return IGMediaId + "/insights?metric=engagement,impressions,reach,saved" 
                    + "&access_token=" + accessToken;
            }
            return null;
        }
        public void UpdatePostInsights(List<StatisticsObject> insightsValue, PostStatistics post)
        {
            if (post.mediaType == "VIDEO") 
            {
                post.engagement = insightsValue[0].values[0].value;
                post.impressions = insightsValue[1].values[0].value;
                post.reach = insightsValue[2].values[0].value;
                post.saved = insightsValue[3].values[0].value;
                post.videoViews = insightsValue[4].values[0].value;
                context.PostStatistics.Update(post);
                context.SaveChanges();
            }
            else 
            {
                post.engagement = insightsValue[0].values[0].value;
                post.impressions = insightsValue[1].values[0].value;
                post.reach = insightsValue[2].values[0].value;
                post.saved = insightsValue[3].values[0].value;
                context.PostStatistics.Update(post);
                context.SaveChanges();
            }
        }
        public void InsightsStories(BusinessAccount account)
        {
            string response, url; List<StoryStatistics> stories; 
            List<StatisticsObject> insights;

            stories = context.StoryStatistics.Where(p => p.accountId == account.businessId).ToList();
            foreach (StoryStatistics story in stories) 
            {
                if ((url = GetURLStoryInsights(story.mediaId, account.longLiveAccessToken)) != null) 
                {
                    if (!string.IsNullOrEmpty(response = service.GetFacebookRequest(url))) 
                    {
                        var data = handler.handle(JsonConvert.DeserializeObject<JObject>(response), "data", JTokenType.Array);
                        if (data != null) 
                        {
                            insights = data.ToObject<List<StatisticsObject>>();
                            UpdateStoryInsights(insights, story);
                        }
                    }
                    else
                        break;
                }
            }
        }
        public string GetURLStoryInsights(string IGMediaId, string accessToken)
        {
            if (CheckTokenAndIG(accessToken, IGMediaId)) 
            {
                return IGMediaId + "/insights?metric=exits,impressions,reach,replies" 
                    + "&access_token=" + accessToken;
            }
            return null;
        }
        public void UpdateStoryInsights(List<StatisticsObject> insights, StoryStatistics story)
        {
            if (insights.Count == 4) 
            {
                // story.exists = insights[0].values[0].value;
                story.impressions = insights[1].values[0].value;
                story.reach = insights[2].values[0].value;
                story.replies = (int)insights[3].values[0].value;
                context.StoryStatistics.Update(story);
                context.SaveChanges();
            }
        }
        public bool CheckTokenAndIG(string accessToken, string IGId)
        {
            if (!string.IsNullOrEmpty(accessToken)) 
            {
                if (!string.IsNullOrEmpty(IGId))
                {
                    return true;
                }
            }
            return false;
        }
    }
}