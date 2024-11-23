using System;
using Serilog;
using Serilog.Core;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using Controllers;
using database.context;
using Models.Statistics;
using InstagramService.Statistics;

namespace Tests.InstagramAccountStatistics.Statistics
{
    [TestFixture]
    public class TestDailyStatistics
    {
        public Context context;
        public DailyStatistics receiver;
        public BusinessAccount account;
        public DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 0, 0);

        public TestDailyStatistics()
        {
            Logger log = new LoggerConfiguration().CreateLogger();
            context = TestMockingContext.GetContext();
            StatisticsService service = new StatisticsService(log);
            this.receiver = new DailyStatistics(service, context, new JsonHandler(log), 4);
            account = TestMockingContext.BusinessAccountEnviroment();
        }
        [Test]
        public void GetDayStatistics()
        {
            CreateDailyStatistics();
            if (TestMockingContext.values.send_request_to_instagram_service)
                receiver.GetStatistics(account);
        }
        [Test]
        public void UpdateStartEndDayStatistics()
        {
            receiver.UpdateDayStatistics(DayStatistics(), account.businessId);
        }
        [Test]
        public void UpdateDayStatistics()
        {
            CreateDailyStatistics();
            List<StatisticsObject> objects = new List<StatisticsObject>();
            StatisticsObject statistics = new StatisticsObject();
            statistics.values = new List<StatisticsValue>();
            statistics.values.Add(new StatisticsValue()
            {
                value = 1,
                end_time = time
            });
            for (int i = 0; i < 7; i++)
            {
                objects.Add(statistics);
            }
            receiver.UpdateDayStatistics(objects, account.businessId);
        }
        public void CreateDailyStatistics()
        {
            DayStatistics last = new DayStatistics();
            last.accountId = account.businessId;
            last.endTime = time.AddDays(-1);
            DayStatistics next = new DayStatistics();
            next.accountId = account.businessId;
            next.endTime = time;
            context.Statistics.Add(last);
            context.Statistics.Add(next);
            context.SaveChanges();
        }
        public List<StatisticsObject> DayStatistics()
        {
            List<StatisticsObject> objects = new List<StatisticsObject>();
            StatisticsObject statistics = new StatisticsObject();
            statistics.values = new List<StatisticsValue>();
            StatisticsValue value = new StatisticsValue()
            {
                value = 1,
                end_time = time
            };
            statistics.values.Add(value);
            statistics.values.Add(value);
            for (int i = 0; i < 7; i++)
            {
                objects.Add(statistics);
            }   
            return objects;
        }
    }
}