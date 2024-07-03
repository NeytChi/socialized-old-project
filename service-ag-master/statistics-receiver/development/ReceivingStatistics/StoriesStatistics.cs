using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using Controllers;
using database.context;
using Models.Statistics;

namespace InstagramService.Statistics
{
    public class StoriesStatistics : IStatistics
    {
        public StoriesStatistics(StatisticsService service, Context context, JsonHandler handler)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
        }
        public StoriesStatistics(StatisticsService service, Context context, JsonHandler handler, int gettingDays)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
            this.gettingDays = gettingDays;
        }
        public JsonHandler handler;
        public StatisticsService service;
        public Context context;
        int gettingDays = -1;

        public void GetStatistics(BusinessAccount account)
        {
            string url, response;

            if ((url = GetURL(account.businessAccountId, account.longLiveAccessToken)) != null) {
                response  = service.GetFacebookRequest(url);
                if (!string.IsNullOrEmpty(response)) {
                    ReceiveStories(JsonConvert.DeserializeObject<JObject>(response), account.businessId);
                }
            }
        }
        public string GetURL(string IGUserId, string accessToken)
        {
            if (CheckTokenAndIG(accessToken, IGUserId))
                return IGUserId + "/stories?fields=media_url,timestamp,id,media_type&access_token=" + accessToken;
            return null;
        }
        public void ReceiveStories(JObject json, long accountId)
        {
            JToken data; List<StoryValues> values;
             
            if ((data = handler.handle(json, "data", JTokenType.Array)) != null) {
                values = data.ToObject<List<StoryValues>>();
                if (gettingDays == -1)
                    SaveStories(values, accountId);
                else
                    SaveStories(values, accountId, DateTime.Now.AddDays(-gettingDays));
            }
        }
        public void SaveStories(List<StoryValues> statistics, long accountId, DateTime from)
        {
            StoryStatistics story;
            foreach(StoryValues value in statistics) {
                if (value.timestamp > from) {
                    if ((story = context.StoryStatistics.Where(s => s.mediaId == value.id 
                        && s.accountId == accountId).FirstOrDefault()) != null) {
                        story.storyUrl = value.media_url;
                        story.storyType = value.media_type;
                        story.timestamp = value.timestamp;
                        context.StoryStatistics.Update(story);
                        context.SaveChanges();
                        handler.log.Information("Update story, id -> " + story.storyId);
                    }
                    else {
                        story = new StoryStatistics() {
                            accountId = accountId,
                            mediaId = value.id,
                            storyUrl = value.media_url,
                            storyType = value.media_type,
                            timestamp = value.timestamp
                        };
                        context.StoryStatistics.Add(story);
                        context.SaveChanges();
                        handler.log.Information("Create a new story, id -> " + story.storyId);
                    }
                }
            }
        }
        public void SaveStories(List<StoryValues> statistics, long accountId)
        {
            StoryStatistics story;
            foreach(StoryValues value in statistics) {
                if ((story = context.StoryStatistics.Where(s => s.mediaId == value.id 
                    && s.accountId == accountId).FirstOrDefault()) != null) {
                    story.storyUrl = value.media_url;
                    story.storyType = value.media_type;
                    story.timestamp = value.timestamp;
                    context.StoryStatistics.Update(story);
                    context.SaveChanges();
                    handler.log.Information("Update story, id -> " + story.storyId);
                }
                else {
                    story = new StoryStatistics() {
                        accountId = accountId,
                        mediaId = value.id,
                        storyUrl = value.media_url,
                        storyType = value.media_type,
                        timestamp = value.timestamp
                    };
                    context.StoryStatistics.Add(story);
                    context.SaveChanges();
                    handler.log.Information("Create a new story, id -> " + story.storyId);
                }
            }
        }
        public bool CheckTokenAndIG(string accessToken, string IGId)
        {
            if (!string.IsNullOrEmpty(accessToken)) {
                if (!string.IsNullOrEmpty(IGId))
                    return true;
            }
            return false;
        }
    }
}
