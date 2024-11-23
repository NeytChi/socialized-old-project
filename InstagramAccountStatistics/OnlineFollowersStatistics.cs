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
    public class OnlineFollowersStatistics : IStatistics
    {
        Logger log;
        int gettingDays;
        public OnlineFollowersStatistics(StatisticsService service, Context context, JsonHandler handler)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
            this.log = handler.log;
            this.gettingDays = 16;
        }
        public OnlineFollowersStatistics(StatisticsService service, Context context, JsonHandler handler, int gettingDays)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
            this.log = handler.log;
            this.gettingDays = gettingDays;
        }
        public JsonHandler handler;
        public StatisticsService service;
        public Context context;

        public void GetStatistics(BusinessAccount account)
        {
            int index = 0;
            JToken data;
            JArray dataArray;
            JObject json;
            string url, response;
            if ((url = GetStartUrl(account.businessAccountId, account.longLiveAccessToken)) != null) {
                do {
                    response = service.GetFacebookRequestByFullUrl(url);
                    if (!string.IsNullOrEmpty(response)) {
                        json = JsonConvert.DeserializeObject<JObject>(response);
                        data = handler.handle(json, "data", JTokenType.Array);
                        dataArray = data.ToObject<JArray>();
                        for (int i = 0; i < dataArray.Count; i++)
                            SaveOnlineFollowers(dataArray[i].ToObject<StatisticsOnlineFollowers>(), account.businessId);
                        url = GetPreviousURL(json);
                    }
                }
                while (++index <= gettingDays);
            }
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
        public string GetStartUrl(string IGId, string accessToken)
        {
            if (CheckTokenAndIG(accessToken, IGId))
                return service.fbDomen + IGId + "/insights/online_followers/lifetime?fields&access_token=" + accessToken + "";
            return null;
        }
        public void SaveOnlineFollowers(StatisticsOnlineFollowers json, long accountId)
        {
            OnlineFollowers followers;
            foreach (OnlineFollowersValues values in json.values) {  
                DateTime endTime = new DateTime(values.end_time.Year, values.end_time.Month, values.end_time.Day, 0, 0, 0);
                for (int i = 0; i < 24; i++) {
                    followers = GetOnlineFollowers(accountId, endTime.AddHours(i));    
                    if (values.value.ContainsKey(i.ToString())) {
                        followers.value = values.value[i.ToString()].ToObject<long>();
                        context.OnlineFollowers.Update(followers);
                    }
                }
            }
            context.SaveChanges();
        }
        public OnlineFollowers GetOnlineFollowers(long accountId, DateTime endTime)
        {
            OnlineFollowers online = context.OnlineFollowers.Where(o
                => o.accountId == accountId
                && o.endTime == endTime).FirstOrDefault();
            if (online == null) {
                online = new OnlineFollowers();
                online.accountId = accountId;
                online.endTime = endTime;
                context.OnlineFollowers.Add(online);
                context.SaveChanges();
                log.Information("Create new online followers instance with end time ->" + endTime.ToString() + ".");
            }
            return online;
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
