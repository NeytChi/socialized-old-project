using System.Linq;
using Serilog.Core;

using Controllers;
using database.context;
using Models.Statistics;
using System.Collections.Generic;

namespace InstagramService.Statistics 
{
    public class Receiver
    {
        StatisticsService service;
        Context context;
        Logger log;
        List<IStatistics> receivers;
        JsonHandler handler;
            
        public Receiver(Logger log)
        {
            this.log = log;
            this.context = new Context(false);
            this.service = new StatisticsService(log);
            this.handler = new JsonHandler(log);
        }
        public void SetUpReceiving()
        {
            receivers = new List<IStatistics>();
            receivers.Add(new DailyStatistics(service, context, handler, -1));
            receivers.Add(new OnlineFollowersStatistics(service, context, handler));
            receivers.Add(new PostingStatistics(service, context, handler));
            receivers.Add(new StoriesStatistics(service, context, handler));
            receivers.Add(new InsightStatistics(service, context, handler));
        }
        public void SetUpReceiving(int gettingDays)
        {
            receivers = new List<IStatistics>();
            receivers.Add(new DailyStatistics(service, context, handler, gettingDays));
            receivers.Add(new OnlineFollowersStatistics(service, context, handler, gettingDays));
            receivers.Add(new PostingStatistics(service, context, handler, gettingDays));
            receivers.Add(new StoriesStatistics(service, context, handler, gettingDays));
            receivers.Add(new InsightStatistics(service, context, handler));
        }
        public void Start(int businessId, int gettingDays)
        {
            BusinessAccount account = context.BusinessAccounts.Where(b 
                => b.businessId == businessId).FirstOrDefault();
            if (account != null) {
                if (gettingDays == -1)
                    CollectStatistics(account);
                else
                    CollectStatistics(account, gettingDays);
            }
            else 
                log.Information("Can't define account by input args.");
        }
        public void CollectStatistics(BusinessAccount account)
        {
            log.Information("Start collect statistics, account id -> " + account.businessId);
            new AccountStatistics(service, context, handler).UpdateBusinessAccount(ref account);
            SetUpReceiving();
            foreach(IStatistics receiver in receivers)
                receiver.GetStatistics(account);
            account.received = true;
            context.BusinessAccounts.Update(account);
            context.SaveChanges();
            log.Information("End collect statistics, account id -> " + account.businessId);
        }
        public void CollectStatistics(BusinessAccount account, int gettingDays)
        {
            log.Information("Start collect statistics by day, account id -> " + account.businessId);
            SetUpReceiving(gettingDays);
            new AccountStatistics(service, context, handler).UpdateBusinessAccount(ref account);
            foreach(IStatistics receiver in receivers)
                receiver.GetStatistics(account);
            account.received = true;
            context.BusinessAccounts.Update(account);
            context.SaveChanges();
            log.Information("End collect statistics by day, account id -> " + account.businessId);
        }   
    }
}