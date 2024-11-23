using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Controllers;
using database.context;
using Models.Statistics;

namespace InstagramService.Statistics
{
    public class AccountStatistics
    {
        public AccountStatistics(StatisticsService service, Context context, JsonHandler handler)
        {
            this.context = context;
            this.service = service;
            this.handler = handler;
        }
        public JsonHandler handler;
        public StatisticsService service;
        public Context context;
        public void UpdateBusinessAccount(ref BusinessAccount account)
        {
            string url, response;

            if ((url = GetURLAccount(account.businessAccountId, account.longLiveAccessToken)) != null) {
                response = service.GetFacebookRequest(url);
                if (!string.IsNullOrEmpty(response)) {
                    ReceiveAccountInfo(JsonConvert.DeserializeObject<JObject>(response), ref account);
                }
            }
        }
        public void ReceiveAccountInfo(JObject json, ref BusinessAccount account)
        {
            JToken followersCount, mediaCount;
            if (((followersCount = handler.handle(json, "followers_count", JTokenType.Integer)) != null)
                && ((mediaCount = handler.handle(json, "media_count", JTokenType.Integer)) != null)) {
                account.followersCount = followersCount.ToObject<long>();
                account.mediaCount = mediaCount.ToObject<int>();
                context.BusinessAccounts.Update(account);
                handler.log.Information("followers count -> " + account.followersCount + " media count -> " + account.mediaCount);
                context.SaveChanges();
            }
        }
        public string GetURLAccount(string IGId, string accessToken)
        {
            if (CheckTokenAndIG(accessToken, IGId))
                return IGId + "?fields=followers_count,media_count&access_token=" + accessToken;
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