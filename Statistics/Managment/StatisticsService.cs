using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using Serilog.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

using socialized;
using Controllers;

namespace InstagramService.Statistics
{
    public class StatisticsService
    {
        string fbAppId;
        string fbAppSecret;
        public readonly string fbDomen = "https://graph.facebook.com/v5.0/";
        public StatisticsService(Logger log)
        {
            this.log = log;
            this.handler = new JsonHandler(log);
            var configuration = Program.serverConfiguration();
            fbAppId = configuration.GetValue<string>("fb_app_id");
            fbAppSecret = configuration.GetValue<string>("fb_app_secret");
        }
        public Logger log;
        public JsonHandler handler;
        public string GetFacebookId(string accessToken)
        {
            JToken id; JObject json; string response;
            
            if ((response = GetFacebookRequest("me?fields=id&access_token=" + accessToken)) != null) {
                json = JsonConvert.DeserializeObject<JObject>(response);
                if ((id = handler.handle(json, "id", JTokenType.String)) != null)
                    return id.ToString();
                else
                    log.Information("Server can't convert json request from FB to FB id. Can't get FB id.");
            }
            return null;
        }
        public List<string> GetAccounts(string facebookId, string accessToken)
        {
            string response;

            if ((response = GetFacebookRequest(facebookId + "/accounts?access_token=" + accessToken)) != null) {
                JObject json = JsonConvert.DeserializeObject<JObject>(response);
                return PullOutFacebookAccounts(json);
            }
            return null;
        }
        public List<string> PullOutFacebookAccounts(JObject response)
        {
            List<string> ids = null;

            JToken data = handler.handle(response, "data", JTokenType.Array);
            if (data != null) {
                JArray dataMassive = data.ToObject<JArray>();
                ids = new List<string>();
                for (int i = 0; i < dataMassive.Count; i++) {
                    JObject dataElement = dataMassive[i].ToObject<JObject>();
                    JToken id = handler.handle(dataElement, "id", JTokenType.String);
                    if (id != null)
                        ids.Add(id.ToObject<string>());
                }
            }
            if (ids == null)
                log.Information("Server can't get data array from accounts FB request.");
            if (ids.Count == 0)
                log.Information("FB account doesn't have any IG account.");
            return ids;
        }
        public string GetBussinessAccountId(string id, string accessToken)
        {
            string url = id + "?fields=instagram_business_account&access_token=" + accessToken;
            string response = GetFacebookRequest(url);
            if (response != null) {
                JObject json = JsonConvert.DeserializeObject<JObject>(response);
                return PullOutBussinessAccountId(json);
            }
            return null;
        }
        public string PullOutBussinessAccountId(JObject response)
        {
            JToken businessAccount = handler.handle(response, "instagram_business_account", JTokenType.Object);
            if (businessAccount != null) {
                JObject accountElement = businessAccount.ToObject<JObject>();
                JToken businessId = handler.handle(accountElement, "id", JTokenType.String);
                if (businessId != null)
                    return businessId.ToString();
            }
            return null;
        }
        public string GetLongTermAccessToken(string accessToken)
        {
            string url = "oauth/access_token?grant_type=fb_exchange_token" +
                "&client_id=" + fbAppId + "&client_secret=" + fbAppSecret + "&fb_exchange_token=" + accessToken;
            string response = GetFacebookRequest(url);
            if (response != null) {
                JToken longAccessToken = handler.handle(JsonConvert.DeserializeObject<JObject>(response), "access_token", JTokenType.String);
                if (longAccessToken != null)
                    return longAccessToken.ToObject<string>();
            }
            return null;
        }
        public BIGAccount GetAccountInfo(string igBusinessAccount, string accessToken)
        {
            string url, response;

            url = igBusinessAccount + "?fields=biography,username,name,profile_picture_url&access_token=" + accessToken;
            if ((response = GetFacebookRequest(url)) != null) {
                JObject json = JsonConvert.DeserializeObject<JObject>(response);
                return json.ToObject<BIGAccount>();
            }
            return null;
        }
        public string GetUsername(string igBusinessAccount, string accessToken)

        {
            string url = igBusinessAccount + "?fields=username&access_token=" + accessToken;
            string response = GetFacebookRequest(url);
            if (response != null) {
                JObject json = JsonConvert.DeserializeObject<JObject>(response);
                JToken username = handler.handle(json, "username", JTokenType.String);
                if (username != null)
                    return username.ToObject<string>();
            }
            return null;
        }
        public string GetFacebookRequest(string url)
        {
            try {
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", 
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                Stream data = client.OpenRead(fbDomen + url);
                StreamReader reader = new StreamReader(data);
                string result = reader.ReadToEnd();
                data.Close();
                reader.Close();
                log.Information("Send GET request.");
                return result;
            }
            catch (Exception e) {
                log.Information("Can't send GET request, ex ->" + e.Message );
            }
            return null;
        }
        public string GetFacebookRequestByFullUrl(string fullUrl)
        {
            try {
                WebClient client = new WebClient();
                client.Headers.Add("user-agent", 
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                Stream data = client.OpenRead(fullUrl);
                StreamReader reader = new StreamReader(data);
                string result = reader.ReadToEnd();
                data.Close();
                reader.Close();
                log.Information("Send GET request.");
                return result;
            }
            catch (Exception e) {
                log.Information("Can't send GET request, ex ->" + e.Message );
            }
            return null;
        }
    }
}