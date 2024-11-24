using Models.Common;
using NUnit.Framework;
using database.context;

using Instasoft.Testing;
using Models.GettingSubscribes;
using Models.SessionComponents;
using System.Collections.Generic;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;

using nmediator;
using ngettingsubscribers;
using InstagramService;
using Managment;

namespace Common
{
    [TestFixture]
    public class TestFiltersGS
    {
        public TestFiltersGS()
        {
            this.context = new Context(true, true);
            filtersGS = FiltersGS.GetInstance(new SessionStateHandler(context));
        }
        private Context context;
        private FiltersGS filtersGS;
        private string message = string.Empty;
        private long userPk = 8273287;
            
        [Test]
        public void CheckAllFilters()
        {
            TestMockingContext.context = context;
            UserCache user = TestMockingContext.CreateUser();
            SessionCache sessionCache = TestMockingContext.CreateSession(user.userId);
            Session session = SessionManager.CreateInstance(context).LoadSession(sessionCache.sessionId);
            TaskFilter filter = new TaskFilter();
            bool unsuccess = filtersGS.CheckByAllFilters(ref session, filter, userPk);
            bool success = filtersGS.CheckByAllFilters(ref session, null, userPk);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void RangeSubscribersFrom()
        {
            bool success = filtersGS.RangeSubscribersFrom(40, 60);
            bool unsuccess = filtersGS.RangeSubscribersFrom(0060, 40);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }   
        [Test]
        public void RangeSubscribersTo()
        {
            bool success = filtersGS.RangeSubscribersTo(60, 40);
            bool unsuccess = filtersGS.RangeSubscribersTo(40, 60);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void RangeFollowingFrom()
        {
            bool success = filtersGS.RangeFollowingFrom(40, 50);
            bool unsuccess = filtersGS.RangeFollowingFrom(60, 50);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void RangeFollowingTo()
        {
            bool success = filtersGS.RangeFollowingTo(60, 50);
            bool unsuccess = filtersGS.RangeFollowingTo(40, 50);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void PublicationCount()
        {
            bool success = filtersGS.PublicationCount(0, 10);
            bool unsuccess = filtersGS.PublicationCount(12, 10);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void WithoutProfilePhoto()
        {
            bool success = filtersGS.WithoutProfilePhoto(true, "123456789");
            bool unsuccess = filtersGS.WithoutProfilePhoto(true, null);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void LatestPublicationNoYonger()
        {
            TestMockingContext.context = context;
            UserCache user = TestMockingContext.CreateUser();
            SessionCache sessionCache = TestMockingContext.CreateSession(user.userId);
            Session session = SessionManager.CreateInstance(context).LoadSession(sessionCache.sessionId);
            InstaFullUserInfo userInfo = new InstaFullUserInfo();
            userInfo.UserDetail = new InstaUserInfo();
            userInfo.UserDetail.MediaCount = 10;
            userInfo.UserDetail.Pk = userPk;
            userInfo.UserDetail.Username = session.User.UserName;
            bool success = filtersGS.LatestPublicationNoYonger(ref session, 1005, userInfo);
            Assert.AreEqual(success, true);
        }
        [Test]
        public void WithProfileUrl()
        {
            bool success = filtersGS.WithProfileUrl(true, "Test @test.");
            bool unsuccess = filtersGS.WithProfileUrl(true, "Test.");
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void LanguagesUkrainian()
        {
            bool success = filtersGS.LanguageUkrainian(true, "Привіт світ!");
            bool unsuccess = filtersGS.LanguageUkrainian(true, "Hello world!");
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void LanguagesEnglish()
        {
            bool success = filtersGS.LanguageEnglish(true, "Hello world!");
            bool unsuccess = filtersGS.LanguageEnglish(true, "Привет мир!");
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void LanguagesArabian()
        {
            bool success = filtersGS.LanguageArabian(true, "مممممممممم!");
            bool unsuccess = filtersGS.LanguageArabian(true, "Привіт світ!");
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void LanguagesRussian()
        {
            bool success = filtersGS.LanguageRussian(true, "Привет мир!");
            bool unsuccess = filtersGS.LanguageRussian(true, "مممممممممم");
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void WordsInDescription()
        {
            string WordsInDescription = "Lorem ipsum dolor sit amet.";
            TaskFilter filter = new TaskFilter();
            filter.words = new List<FilterWord>();
            FilterWord word = new FilterWord();
            word.wordUse = true;
            word.wordValue = "Lorem";
            filter.words.Add(word);
            bool success = filtersGS.WordsInDescription(filter, WordsInDescription);
            filter.words.Remove(word);
            word.wordValue = "Hello";
            filter.words.Add(word);
            bool unsuccess = filtersGS.WordsInDescription(filter, WordsInDescription);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void NoWordsInDescription()
        {
            string noWordsInDescription = "Lorem ipsum dolor sit amet.";
            TaskFilter filter = new TaskFilter();
            filter.words = new List<FilterWord>();
            FilterWord word = new FilterWord();
            word.wordUse = false;
            word.wordValue = "Hello";
            filter.words.Add(word);
            bool success = filtersGS.NoWordsInDescription(filter, noWordsInDescription);
            filter.words.Remove(word);
            word.wordValue = "Lorem";
            filter.words.Add(word);
            bool unsuccess = filtersGS.NoWordsInDescription(filter, noWordsInDescription);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}