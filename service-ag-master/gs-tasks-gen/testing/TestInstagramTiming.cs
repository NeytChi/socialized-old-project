using System;
using Serilog;
using NUnit.Framework;
using database.context;
using Instasoft.Testing;
using Models.GettingSubscribes;
using Models.SessionComponents;
using System.Collections.Generic;

namespace InstagramService.Test
{
    [TestFixture]
    public class TestInstagramTiming
    {
        public InstagramTiming instance = new InstagramTiming(new LoggerConfiguration().CreateLogger());
        [Test]
        public void GetBiggerActionTime()
        {
            ICollection<TaskAction> actions = new List<TaskAction>();
            TaskAction action = new TaskAction();
            action.actionNumber = 1;
            actions.Add(action);
            int success = instance.GetBiggerActionTime(actions, false);
            int unsuccess = instance.GetBiggerActionTime(null, false);
            Assert.AreEqual(success, instance.times.followFrom * 1000);
            Assert.AreEqual(unsuccess, 0);
        } 
        [Test]
        public void GetMillisecondsByAction()
        {
            int success = instance.GetMillisecondsByAction(TaskActions.Follow, false);
            int unsuccess = instance.GetMillisecondsByAction(TaskActions.Unknow, false);
            Assert.AreEqual(success, instance.times.followFrom * 1000);
            Assert.AreEqual(unsuccess, 0);
        } 
        [Test]
        public void GetFromOld()
        {
            int success = instance.GetFromOld(TaskActions.Follow);
            int unsuccess = instance.GetFromOld(TaskActions.Unknow);
            Assert.AreEqual(success, instance.times.oldFollowFrom);
            Assert.AreEqual(unsuccess, 0);
        }
        [Test]
        public void GetFromNonOld()
        {
            int success = instance.GetFromNonOld(TaskActions.Follow);
            int unsuccess = instance.GetFromNonOld(TaskActions.Unknow);
            Assert.AreEqual(success, instance.times.followFrom);
            Assert.AreEqual(unsuccess, 0);
        }
        [Test]
        public void GetCountByAction()
        {
            int success = instance.GetCountByAction(TaskActions.Follow, false);
            int unsuccess = instance.GetCountByAction(TaskActions.Unknow, false);
            Assert.AreEqual(success, instance.times.followCount);
            Assert.AreEqual(unsuccess, 0);
        }
        [Test]
        public void GetCountOld()
        {

            int success = instance.GetCountOld(TaskActions.Follow);
            int unsuccess = instance.GetCountOld(TaskActions.Unknow);
            Assert.AreEqual(success, instance.times.oldFollowCount);
            Assert.AreEqual(unsuccess, 0);
        }
        [Test]
        public void GetCountNonOld()
        {
            int success = instance.GetCountNonOld(TaskActions.Follow);
            int unsuccess = instance.GetCountNonOld(TaskActions.Unknow);
            Assert.AreEqual(success, instance.times.followCount);
            Assert.AreEqual(unsuccess, 0);
        }
        [Test]
        public void CompareActionCount()
        {
            TimesAction times = new TimesAction();
            times.followCount = 1000;
            bool success = instance.CompareActionCount(TaskActions.Follow, times);
            times.followCount = 100;
            bool unsuccess = instance.CompareActionCount(TaskActions.Follow, times);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetNonUpdatedTimesAction()
        {
            Context context = new Context(true, true);
            TestMockingContext.context = context;
            SessionCache session = TestMockingContext.CreateSession(TestMockingContext.CreateUser().userId);
            session.Times.followCount = 1000;
            session.Times.followLastAt = DateTime.Now.AddDays(-2);
            context.TimesAction.Update(session.Times);
            context.SaveChanges();
            List<TimesAction> success = instance.GetNonUpdatedTimesAction(context);
            List<TimesAction> unsuccess = instance.GetNonUpdatedTimesAction(null);
            Assert.AreEqual(success.Count, 1);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void UpdateToStart()
        {
            Context context = new Context(true, true);
            TestMockingContext.context = context;
            SessionCache session = TestMockingContext.CreateSession(TestMockingContext.CreateUser().userId);
            List<TimesAction> times = new List<TimesAction>();
            times.Add(session.Times);
            instance.UpdateToStart(context, times);
            instance.UpdateToStart(null, null);
        }
        [Test]
        public void UpdateCountToStart()
        {
            Context context = new Context(true, true);
            TestMockingContext.context = context;
            SessionCache session = TestMockingContext.CreateSession(TestMockingContext.CreateUser().userId);
            instance.UpdateCountToStart(context, session.Times, DateTime.Now);
            instance.UpdateCountToStart(null, null, DateTime.Now);
        }
    }
}