using System;
using System.Linq;
using System.Collections.Generic;
using Serilog.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Controllers;
using database.context;
using Models.Statistics;

namespace InstagramService.Statistics
{
    public class DailyStatistics : IStatistics
    {
        Logger log;
        int gettingDays;
        public DailyStatistics(StatisticsService service, JsonHandler handler)
        {
            this.context = new Context(false);
            this.service = service;
            this.handler = handler;
            this.log = handler.log;
        }
        public DailyStatistics(StatisticsService service, Context context, JsonHandler handler, int gettingDays)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
            this.log = handler.log;
            this.gettingDays = gettingDays;
            if (this.gettingDays == -1)
                this.gettingDays = 360;
        }
        public JsonHandler handler;
        public StatisticsService service;
        public Context context;

        public void GetStatistics(BusinessAccount account)
        {
            int index = 0;
            string url,response;

            if ((url = GetStartUrl(account.businessAccountId, account.longLiveAccessToken)) != null) {
                do {
                    response = service.GetFacebookRequestByFullUrl(url);
                    if (!string.IsNullOrEmpty(response)) {
                        var json = JsonConvert.DeserializeObject<JObject>(response);
                        var data = handler.handle(json, "data", JTokenType.Array);
                        var statisticsData = data.ToObject<List<StatisticsObject>>();
                        UpdateDayStatistics(statisticsData, account.businessId);
                        url = GetPreviousURL(json);
                    }              
                }
                while (++index <= gettingDays);
            }
        }
        public void UpdateDayStatistics(List<StatisticsObject> statistics, long businessId)
        {
            DayStatistics daily; DateTime endTime;

            if (statistics.Count == 9) {
                for (int i = 0; i < statistics[0].values.Count; i++) {
                    endTime = statistics[0].values[i].end_time;
                    daily = context.Statistics.Where(d => d.endTime.Day == endTime.Day
                        && d.endTime.Month == endTime.Month
                        && d.endTime.Year == endTime.Year
                        && d.accountId == businessId).FirstOrDefault();
                    if (daily == null) {
                        daily = new DayStatistics() {
                            accountId = businessId,
                            endTime = endTime
                        };
                        context.Statistics.Add(daily);
                        context.SaveChanges();
                    }
                    daily.followerCount = (int)statistics[0].values[i].value;
                    daily.emailContacts = (int)statistics[1].values[i].value;
                    daily.profileViews = statistics[2].values[i].value;
                    daily.getDirectionsClicks = (int)statistics[3].values[i].value;
                    daily.phoneCallClicks = (int)statistics[4].values[i].value;
                    daily.textMessageClicks = (int)statistics[5].values[i].value;
                    daily.websiteClicks = (int)statistics[6].values[i].value;
                    daily.impressions = (int)statistics[7].values[i].value;
                    daily.reach = (int)statistics[8].values[i].value;
                    context.Statistics.Update(daily);
                    context.SaveChanges();
                    service.log.Information("Update day statistics. Timestamp -> " + daily.endTime);
                }
            }
        }
        public string GetStartUrl(string IGId, string accessToken)
        {
            if (CheckTokenAndIG(accessToken, IGId)) {
                return service.fbDomen + IGId + "/insights?" +
                "metric=follower_count%2Cemail_contacts%2Cprofile_views%2Cget_directions_clicks%2C%20" + 
                "phone_call_clicks%2Cprofile_views%2Ctext_message_clicks%2Cwebsite_clicks%2Cimpressions%2Creach" + 
                "&period=day" +
                "&access_token=" + accessToken;
            }
            return null;
        }
        public string GetPreviousURL(JObject json)
        {
            JToken paging;
            JToken previous;

            if ((paging = handler.handle(json, "paging", JTokenType.Object)) != null) {
                if ((previous = handler.handle(paging.ToObject<JObject>(), "previous", JTokenType.String)) != null) {
                    return previous.ToString();
                }
            }
            log.Warning("Server can't get previous url in online followers json.");
            return null;
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
