using System;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Microsoft.AspNetCore.Mvc;

using Managment;
using database.context;
using Models.Common;
using Models.Statistics;
using Models.SessionComponents;
using InstagramService.Statistics;

namespace Controllers
{
    [Route("v1.0/[controller]/[action]/")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly Context context;
        public ManagerStatistics manager;
        public GetterStatistics getter;
        private PackageCondition access;

        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
		
        public StatisticsController(Context context)
        {
            this.context = context;
            this.manager = new ManagerStatistics(log, context);
            this.getter = new GetterStatistics(new Context(false));
            this.access = new PackageCondition(context, log);
        }
        [HttpPost]
        [ActionName("FacebookAccounts")]
        public ActionResult<dynamic> FacebookAccounts(StatisticsCache cache)
        {
            string message = string.Empty;
            List<BIGAccount> accounts;
            User user;
            
            if ((user = context.Users.Where(u => u.userToken == cache.user_token).FirstOrDefault()) != null) {
                if ((accounts = manager.GetFacebookAccounts(user.userId, cache.access_token, ref message)) != null) {
                    if (accounts.Count > 0) 
                        return new { success = true, data = new { 
                            long_live_token = manager.service.GetLongTermAccessToken(cache.access_token),
                            accounts = accounts 
                        } };
                    else {
                        log.Information("Server can't get any new BIG account.");
                        message = "zero_accounts";
                    }
                }
            }
            else {
                log.Information("Server can't define user by promotion token.");
                message = "define_token";
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("AddAccount")]
        public ActionResult<dynamic> AddAccounts(StatisticsCache cache)
        {
            string message = null;
            User user;

            if ((user = context.Users.Where(u => u.userToken == cache.user_token).FirstOrDefault()) != null) {
                List<BusinessAccount> accounts = manager.CreateBusinessAccounts(cache.access_token, 
                    user, cache.account_ids, ref message);
                if (accounts != null && accounts.Count > 0)
                    return new { success = true, data = accounts.Select(acc => 
                        new {
                            account_id = acc.businessId,
                            account_name = acc.accountUsername,
                            created_at = acc.createdAt,
                            received = acc.received,
                            profile_picture = acc.profilePicture
                        }) };
                else {
                    log.Information("Server can't get any new BIG account.");
                    message = "zero_accounts";
                }
            }
            else {
                log.Information("Server can't define user by promotion token.");
                message = "define_token";
            }
            return Return500Error(message); 
        }
        [HttpGet]
        [ActionName("Accounts")]
        public ActionResult<dynamic> Accounts()
        {
            string userToken;
            
            userToken = HttpContext?.Request.Headers.Where(h 
                => h.Key == "Authorization").Select(h => h.Value)
                .FirstOrDefault() 
                ?? "Bearer ";
            if (!string.IsNullOrEmpty(userToken) && userToken.Contains("Bearer "))
                userToken = userToken.Remove(0, 7);     
            return new { success = true, data = new { accounts = manager.GetUserAccounts(userToken) } };
        }
        [HttpPost]
        [ActionName("Start")]
        public ActionResult<dynamic> Start(StatisticsCache cache)
        {
            string message = string.Empty;
            User user; BusinessAccount account;
            int gettingDays = 0;

            if ((user = context.Users.Where(u => u.userToken == cache.user_token).FirstOrDefault()) != null) {
                if ((gettingDays = access.AnalyticsDays(user.userId, ref message)) != 0) {
                    if ((account = manager.GetNonDeleteBusinessAccount(user.userId, cache.account_id, ref message)) != null) {
                        if (!account.startProcess && !account.received) {
                            manager.StartReceive(account, gettingDays);
                            return new { success = true, 
                                message = GetMessage("end_statistics", Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US") };
                        }
                        else {
                            log.Information("Statistics for the current account are already being collected.");
                            message = "exist_statistics";
                        }
                    }
                }
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("ByTime")]
        public ActionResult<dynamic> ByTime(StatisticsCache cache)
        {
            string message = null;    
            BusinessAccount account = manager.GetNonDeleteAccount(cache.user_token, cache.account_id, ref message);
            if (account != null)
                return new { success = true, data = getter.SelectByTime(account, cache.from, cache.to) };
            return Return500Error(message);       
        }
        [HttpPost]
        [ActionName("RemoveAccount")]
        public ActionResult<dynamic> Remove(StatisticsCache cache)
        {
            string message = null;
            BusinessAccount account = manager.GetNonDeleteAccount(cache.user_token, cache.account_id, ref message);
            if (account != null) {
                if (manager.RemoveAccount(cache.user_token, cache.account_id, ref message))
                    return new { success = true };
            }
            return Return500Error(message);       
        }
        [HttpPost]
        [ActionName("GeneratePDF")]
        public ActionResult<dynamic> GeneratePDF(StatisticsCache cache)
        {
            string pdfUrl, message = null;
            BusinessAccount account = manager.GetNonDeleteAccount(cache.user_token, cache.account_id, ref message);
            if (account != null) {
                if (!account.startProcess && account.received) {
                    if (!string.IsNullOrEmpty(pdfUrl = manager.GeneratePDF(account, cache.from, cache.to)))
                        return new { success = true, data = new { pdf_url = pdfUrl }};
                    else
                        message = "This account can't get a statistics data in pdf.";
                }
                else{
                    log.Information("Statistics for the current account are already being collected.");
                    message = "exist_statistics";
                }
            }
            return Return500Error(message);
    
        }
        public dynamic Return500Error(string key)
        {
            string culture = "en_US";
            if (Response != null)
                Response.StatusCode = 500;
            if (Request.Headers.ContainsKey("Accept-Language")) {
                culture = Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US";
            }
            log.Warning(key + " IP -> " + 
                HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new { success = false, 
                message = GetMessage(key, culture)
            };
        }
        public string GetMessage(string key, string culture)
        {
            string value;
            
            value = context.Cultures.Where(c
                => c.cultureKey == key
                && c.cultureName == culture).FirstOrDefault()?.cultureValue ?? null;
            if (value == null)
                value = context.Cultures.Where(c 
                => c.cultureKey == key
                && c.cultureName == "en_US").First().cultureValue;
            return value;
        }
    }
    public struct StatisticsCache
    {
        public string user_token;
        public long session_id;
        public string access_token;
        public DateTime from;
        public DateTime to;
        public long account_id;
        public string[] account_ids;
    }
} 