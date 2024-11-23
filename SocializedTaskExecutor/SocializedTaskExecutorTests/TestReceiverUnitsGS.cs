using NUnit.Framework;
using database.context;
using Instasoft.Testing;
using Models.GettingSubscribes;
using System.Collections.Generic;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.API.Builder;
using ngettingsubscribers;
using Managment;

namespace InstagramService.Test
{
    [TestFixture]
    public class TestReceiverUnitsGS
    {
        public TestReceiverUnitsGS()
        {
            this.context = new Context(true, true);
            this.receiver = ReceiverUnitsGS.GetInstance();
            this.receiver.context = context;
            TestMockingContext.context = context; 
            data = TestMockingContext.CreateTaskDataEnviroment();
            session = SessionManager.CreateInstance(context).LoadSession(1);
        }
        public Session session;
        public TaskData data;
        public Context context;
        public ReceiverUnitsGS receiver;

        [Test]
        public void SetInstagramUnits()
        {
            data.dataNames = "thestandardperth";
            bool success = receiver.SetInstagramUnits(ref session, data, TaskSubtype.ByUserFollowers);
            bool unsuccess = receiver.SetInstagramUnits(ref session, null, TaskSubtype.ByUserFollowers);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetSaveUsers()
        {
            data.dataNames = "thestandardperth";
            bool success = receiver.GetSaveUsers(ref session, data, TaskSubtype.ByUserFollowers);
            bool unsuccess = receiver.GetSaveUsers(ref session, null, TaskSubtype.ByUserFollowers);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetSaveUser()
        {
            data.dataNames = "thestandardperth";
            bool success = receiver.GetSaveUser(ref session, data);
            bool unsuccess = receiver.GetSaveUser(ref session, null);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetSaveFollowing()
        {
            data.dataNames = "thestandardperth";
            Session session = SessionManager.CreateInstance(context).LoadSession(1);
            bool success = receiver.GetSaveFollowing(ref session, data);
            bool unsuccess = receiver.GetSaveFollowing(ref session, null);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void GetUsersByCommentators()
        {
            var success = receiver.GetUsersByCommentators(ref session, "thestandardperth", 0);
            var unsuccess = receiver.GetUsersByCommentators(ref session, null, 0);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void SortUsersFromComment()
        {
            List<InstaComment> comments = new List<InstaComment>();
            comments.Add(new InstaComment());
            var success = receiver.SortUsersFromComment(comments);
            var unsuccess = receiver.SortUsersFromComment(null);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetMediaComments()
        {
            var medias = receiver.GetUserMedia(ref session, "thestandardperth", 0);
            var success = receiver.GetMediaComments(ref session, medias[0].Pk, 0);
            var unsuccess = receiver.GetMediaComments(ref session, null, 0);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUserMedia()
        {
            var success = receiver.GetUserMedia(ref session, "nikachikapika", 0);
            var unsuccess = receiver.GetUserMedia(ref session, null, 0);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetCommentsByCommentators()
        {
            var success = receiver.GetCommentsByCommentators(ref session, "thestandardperth", 0);
            var unsuccess = receiver.GetCommentsByCommentators(ref session, null, 0);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUsersLikers()
        {
            var success = receiver.GetUsersLikers(ref session, "nikachikapika", 0);
            var unsuccess = receiver.GetUsersLikers(ref session, null, 0);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetLikers()
        {
            var medias = receiver.GetUserMedia(ref session, session.User.UserName, 0);
            var success = receiver.GetLikers(ref session, medias[0].Pk);
            var unsuccess = receiver.GetLikers(ref session, null);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUsersByLocation()
        {
            data.dataNames = "London";
            List<InstaUserShort> success = receiver.GetUsersByLocation(ref session, data);
            List<InstaUserShort> unsuccess = receiver.GetUsersByLocation(ref session, null);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void SortUsersFromMedia()
        {
            InstaSectionMedia medias = new InstaSectionMedia();
            medias.Medias = new List<InstaMedia>();
            InstaMedia media = new InstaMedia();
            media.User = new InstaUser(new InstaUserShort());
            medias.Medias.Add(media);
            var success = receiver.SortUsersFromMedia(medias);
            var unsuccess = receiver.SortUsersFromMedia(null);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void SearchLocation()
        {
            var success = receiver.SearchLocation(ref session, 0, 0, "London");
            var unsuccess = receiver.SearchLocation(ref session, 0, 0, null);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetRecentLocationFeeds()
        {
            InstaLocationShortList locations = receiver.SearchLocation(ref session, 0, 0, "London");
            var success = receiver.GetRecentLocationFeeds(ref session, locations[0].ExternalId, 0);
            var unsuccess = receiver.GetRecentLocationFeeds(ref session, null, 0);
            Assert.AreEqual(success.Medias.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetUsersByHashtag()
        {
            var success = receiver.GetUsersByHashtag(ref session, "spring", 0);
            var unsuccess = receiver.GetUsersByHashtag(ref session, null, 0);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetFollowing()
        {
            var success = receiver.GetFollowing(ref session, "nikachikapika", 0);
            var unsuccess = receiver.GetFollowing(ref session, null, 0);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetFollowers()
        {
            List<InstaUserShort> success = receiver.GetFollowers(ref session, "nikachikapika", 0);
            List<InstaUserShort> unsuccess = receiver.GetFollowers(ref session, null, 0);
            Assert.AreEqual(success.Count > 0, true);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void RemoveExcessUsers()
        {
            List<InstaUserShort> users = new List<InstaUserShort>();
            InstaUserShort user = new InstaUserShort();
            user.Pk = 1;
            user.UserName = "test";
            users.Add(user);
            TaskData data = TestMockingContext.CreateTaskDataEnviroment();
            data.dataNames = "test";
            context.TaskData.Update(data);
            context.SaveChanges();
            var success = receiver.RemoveExcessUsers(users, data.taskId);
            var unsuccess = receiver.RemoveExcessUsers(null, 1);
            Assert.AreNotEqual(success, null);
            Assert.AreEqual(success.Count, 0);
            Assert.AreEqual(unsuccess, null);
        }
    }
}