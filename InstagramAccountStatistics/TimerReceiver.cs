using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Serilog;
using Serilog.Core;
using database.context;
using Models.Statistics;

namespace InstagramService.Statistics
{
    public class TimerReceiver
    {
        Timer checker;
        Logger log;
        Semaphore semaphore = new Semaphore(1, 1);
        StatisticsService service;
        int millisecondsToCheck = 86400000;
        public TimerReceiver(Logger log) 
        {
            this.log = log;
            service = new StatisticsService(log);
            log.Information("Start auto-posting service.");
            checker = new Timer(Checking, null, 0, millisecondsToCheck);
            semaphore.WaitOne();
            semaphore.WaitOne();
        }
        public void Checking(object input)
        {
            CheckAccessTokens();
            CheckToUpdate();
        }
        public void CheckAccessTokens()
        {
            Context context = new Context(false);
            BusinessAccount[] accounts = context.BusinessAccounts.Where(x => DateTime.Now.AddDays(1) > x.longTokenExpiresIn
                && !x.deleted).ToArray();
            foreach (BusinessAccount account in accounts) {
                account.longLiveAccessToken = service.GetLongTermAccessToken(account.longLiveAccessToken);
                account.longTokenExpiresIn = DateTime.Now.AddDays(60);
            }
            context.BusinessAccounts.UpdateRange(accounts);
            context.SaveChanges();
            log.Information("Check access token on business accounts.");
        }
        public void CheckToUpdate()
        {
            Context context = new Context(false);
            Receiver receiver = new Receiver(log);
            BusinessAccount[] accounts = context.BusinessAccounts.Where(x => !x.deleted).ToArray();
            foreach (BusinessAccount account in accounts)
                receiver.CollectStatistics(account, 1);
            log.Information("Check to update statistics of business accounts.");
        }
    }
}