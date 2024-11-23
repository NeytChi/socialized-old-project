using System;
using System.Web;
using System.Linq;
using Serilog.Core;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Common;
using socialized;
using database.context;
using Models.Statistics;
using Models.Common;
using Models.SessionComponents;

namespace InstagramService.Statistics
{
    public class ManagerStatistics
    {
        private Semaphore pool;
        public Context context;
        public StatisticsService service;
        public AwsUploader uploader;
        public Logger log;
        public string statisticsReceiver;
        public ManagerStatistics(Logger log, Context context)
        {
            var configuration = Program.serverConfiguration();
            statisticsReceiver = configuration.GetValue<string>("statistics_receiver_fullpath");
            int receiversCount = configuration.GetValue<int>("statistics_receivers_count");
            this.log = log;
            this.context = context;
            this.service = new StatisticsService(log);
            this.uploader = new AwsUploader(log);
            this.pool = new Semaphore(receiversCount, receiversCount);
        }
        public IGAccount GetNonDeleteSession(string userToken, long accountId, ref string message)
        {
            if (!string.IsNullOrEmpty(userToken)) {
                IGAccount account = (from s in context.IGAccounts
                join user in context.Users on s.userId equals user.userId
                where userToken == user.userToken
                    && s.accountId == accountId
                    && s.accountDeleted == false
                select s).FirstOrDefault();
                if (account == null)
                    message = "Server can't define ig account."; 
                return account;
            }
            else
                message = "User token is null or empty.";
            return null;
        }
        public BusinessAccount GetNonDeleteBusinessAccount(long userId, long businessId, ref string message)
        {
            BusinessAccount businessAccount = (from baccount in context.BusinessAccounts
                join user in context.Users on baccount.userId equals user.userId
            where userId == user.userId
                && baccount.businessId == businessId
                && baccount.deleted == false
            select baccount).FirstOrDefault();
            if (businessAccount == null) {
                log.Information("Server can't define business account.");
                message = "unknow_account";
            }
            return businessAccount;
        }
        public BusinessAccount GetNonDeleteAccount(string userToken, long businessId, ref string message)
        {
            if (!string.IsNullOrEmpty(userToken)) {
                BusinessAccount businessAccount = (from baccount in context.BusinessAccounts
                join user in context.Users on baccount.userId equals user.userId
                where user.userToken == userToken
                    && baccount.businessId == businessId
                    && baccount.deleted == false 
                    select baccount).FirstOrDefault();
                if (businessAccount == null){
                    log.Information("Server can't define business account.");
                    message = "unknow_account";
                }
                return businessAccount;
            }
            else {
                log.Information("User token is null or empty.");
                message = "define_token";
            }
            return null;
        }
        public List<BIGAccount> GetFacebookAccounts(int userId, string accessToken, ref string message)
        {
            string facebookId, businessId;
            List<string> ids;
            List<BIGAccount> accounts = null;
            BIGAccount account;

            if ((facebookId = service.GetFacebookId(accessToken)) != null) {
                if ((ids = service.GetAccounts(facebookId, accessToken)).Count >= 1) {
                    accounts = new List<BIGAccount>();
                    foreach (string id in ids) {
                        businessId = service.GetBussinessAccountId(id, accessToken);
                        if ((GetBusinessAccount(userId, businessId, ref message)) == null ) { 
                            if ((account = service.GetAccountInfo(businessId, accessToken)) != null)
                                accounts.Add(account);
                        }
                    }
                }
                else {
                    log.Information("Can't define IG accounts id by access token.");
                    message = "define_big";
                }
            }
            else {
                message = "unknow_token";
                log.Warning("Server can't define facebook id by access token.");
            }
            return accounts;
        }
        public List<BusinessAccount> CreateBusinessAccounts(string accessToken, User user, string[] account_ids, ref string message)
        {
            string facebookId, longLiveAccessToken;
            BIGAccount bIGAccount;
            List<BusinessAccount> accounts = new List<BusinessAccount>();

            if ((facebookId = service.GetFacebookId(accessToken)) != null) {
                longLiveAccessToken = service.GetLongTermAccessToken(accessToken);
                for (int i = 0; i < account_ids.Length; i++) {
                    if (GetBusinessAccount(user.userId, account_ids[i], ref message) == null) {
                        if ((bIGAccount = service.GetAccountInfo(account_ids[i], longLiveAccessToken)) != null) {
                            BusinessAccount account = new BusinessAccount();
                            account.facebookId = facebookId;
                            account.businessAccountId = account_ids[i];
                            account.accountUsername = bIGAccount.username;
                            account.profilePicture = HttpUtility.UrlDecode(bIGAccount.profile_picture_url);
                            account.longLiveAccessToken = longLiveAccessToken;
                            accounts.Add(SaveBusinessAccount(account, longLiveAccessToken, user.userId));
                        }
                        else {
                            log.Information("Can't define business account id by access token.");
                            message = "define_big";
                        }
                    }
                    else {
                        log.Information("This business Instagram account has already exist.");
                        message = "exist_account";
                    }
                }
            }
            else {
                message = "unknow_token";
                log.Warning("Server can't define facebook id by access token.");
            }
            return accounts;
        }
        public BusinessAccount GetBusinessAccount(long userId, string businessAccountId, ref string message)
        {
            BusinessAccount account = context.BusinessAccounts.Where(b 
                => b.userId == userId
                && b.businessAccountId == businessAccountId
                && !b.deleted).FirstOrDefault();
            if (account == null) {
                log.Information("Server can't define business account.");
                message = "unknow_account";
            }
            return account;
        }
        public BusinessAccount SaveBusinessAccount(BusinessAccount account, string accessToken, int userId)
        {
            account.accessToken = accessToken;
            account.userId = userId;
            account.createdAt = DateTime.Now;
            account.tokenCreated = DateTime.Now;
            account.longTokenExpiresIn = DateTime.Now.AddDays(60);
            account.received = false;
            context.BusinessAccounts.Add(account);
            context.SaveChanges();
            log.Information("Save new business account -> " + account.businessId);
            return account;
        }
        public bool RemoveAccount(string userToken, long accountId, ref string message)
        {
            BusinessAccount account = GetNonDeleteAccount(userToken, accountId, ref message);
            if (account != null) {
                account.deleted = true;
                context.BusinessAccounts.Update(account);
                context.SaveChanges();
            }
            return account != null ? account.deleted : false;
        }
        public void StartReceive(BusinessAccount account, int gettingDays)
        {
            account.startProcess = true;
            account.startedProcess = DateTime.Now;
            context.BusinessAccounts.Update(account);
            context.SaveChanges();
            log.Information("Start process receiving statistics, id -> " + account.businessId);
            pool.WaitOne();
            try {
                using (Process process = new Process()) {
                    process.StartInfo.FileName = statisticsReceiver;
                    process.StartInfo.Arguments = "-s " + account.businessId + " -d " + gettingDays;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception e) {
                log.Information("Can't start receive statistics or process has been shut down. ex-message:" + e.Message);
            }
            pool.Release();
            account.received = true;
            account.startProcess = false;
            context.BusinessAccounts.Attach(account).Property(b => b.received).IsModified = true;
            context.SaveChanges();
            context.BusinessAccounts.Attach(account).Property(b => b.startProcess).IsModified = true;
            context.SaveChanges();
            log.Information("End process receiving statis1tics, id -> " + account.businessId);
        }
        public string GeneratePDF(BusinessAccount account, DateTime from, DateTime to)
        {
            string pdfName = account.businessId + "-" + from.Year + "-" + from.Month + "-" + from.Day
                + "--" + to.Year + "-" + to.Month + "-" + to.Day;
            PdfStatistics pdfGen = new PdfStatistics(log, new GetterStatistics(context));
            pdfGen.FillingData(pdfName, account, from ,to);
            uploader.SaveTo(pdfGen.savingPath + pdfName + ".pdf", "analytics/" + pdfName + ".pdf" );
            System.IO.File.Delete(pdfGen.savingPath + pdfName + ".pdf");
            return pdfGen.awsUrl + "analytics/" + pdfName + ".pdf";
        }
        public dynamic GetUserAccounts(string userToken)
        {
            log.Information("Get user's accounts by token");
            return (from acc in context.BusinessAccounts
                join user in context.Users on acc.userId equals user.userId
                where user.userToken == userToken
                    && !acc.deleted
                select
                    new {
                        account_id = acc.businessId,
                        account_name = acc.accountUsername,
                        created_at = acc.createdAt,
                        received = acc.received,
                        profile_picture = acc.profilePicture
                    }
            ).ToList();
        }
    }
    public class BIGAccount
    {
        public string id;
        public string username;
        public string name;
        public string biography;
        public string profile_picture_url;
    }
}