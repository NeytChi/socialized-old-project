using NUnit.Framework;
using database.context;
using Instasoft.Testing;
using Models.GettingSubscribes;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes.Models;
using ngettingsubscribers;
using Managment;

namespace InstagramService.Test
{
    [TestFixture]
    public class TestReceiverMediaGS
    {
        public TestReceiverMediaGS()
        {
            this.context = new Context(true, true);
            this.receiver = ReceiverMediaGS.GetInstance();
            TestMockingContext.context = context; 
        }
        public Session session;
        public UnitGS unit;
        public Context context;
        public ReceiverMediaGS receiver;
        [Test]
        public void SaveMedias()
        {
            InstaMediaList medias = new InstaMediaList();
            InstaMedia media = new InstaMedia();
            media.Pk = "1";
            medias.Add(media);
            receiver.SaveMedias(context, medias, unit.unitId);
        }
        [Test]
        public void SaveMedia()
        {
            receiver.SaveMedia(context, "1", unit.unitId, 2);
            receiver.SaveMedia(null, null, unit.unitId, 2);
        }
        [Test]
        public void RemoveExcessMedia()
        {
            MediaGS mediaGS = TestMockingContext.CreateMediaGSEnviroment();
            session = SessionManager.CreateInstance(context).LoadSession(mediaGS.unit.Data.Task.userId);
            InstaMediaList medias = new InstaMediaList();
            InstaMedia media = new InstaMedia();
            media.Pk = "1";
            medias.Add(media);
            var success = receiver.RemoveExcessMedia(context, medias, mediaGS.unitId);
            var unsuccess = receiver.RemoveExcessMedia(null, null, mediaGS.unitId);
            Assert.AreEqual(success.Count, 0);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetNonHandledMedia()
        {
            MediaGS mediaGS = TestMockingContext.CreateMediaGSEnviroment();
            var success = receiver.GetNonHandledMedia(context, mediaGS.unitId);
            var unsuccess = receiver.GetNonHandledMedia(null, mediaGS.unitId);
            Assert.AreEqual(success.mediaId, mediaGS.mediaId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetHandledMedia()
        {
            MediaGS mediaGS = TestMockingContext.CreateMediaGSEnviroment();
            mediaGS.mediaHandled = true;
            context.Medias.Update(mediaGS);
            context.SaveChanges();
            var success = receiver.GetHandledMedia(context, mediaGS.unitId);
            var unsuccess = receiver.GetHandledMedia(null, mediaGS.unitId);
            Assert.AreEqual(success.mediaId, mediaGS.mediaId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetMediaGS()
        {
            unit = TestMockingContext.CreateUnitGSEnviroment();
            session = SessionManager.CreateInstance(context).LoadSession(unit.Data.Task.sessionId);
            unit.username = session.User.UserName;
            var success = receiver.GetMediaGS(context, unit, ref session, 2);
            var unsuccess = receiver.GetMediaGS(null, null, ref session, 2);
            Assert.AreEqual(success.unitId, unit.unitId);
            Assert.AreEqual(unsuccess, null);
        }
        [Test]
        public void GetSaveUserMedia()
        {
            unit = TestMockingContext.CreateUnitGSEnviroment();
            session = SessionManager.CreateInstance(context).LoadSession(unit.Data.Task.userId);
            unit.username = session.User.UserName;
            bool success = receiver.GetSaveUserMedia(context, ref session, unit, 2);
            bool unsuccess = receiver.GetSaveUserMedia(context, ref session, null, 2);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void RemoveExtraMedia()
        {
            MediaGS mediaGS = TestMockingContext.CreateMediaGSEnviroment();
            session = SessionManager.CreateInstance(context).LoadSession(mediaGS.unit.Data.Task.userId);
            InstaMediaList medias = new InstaMediaList();
            InstaMedia media = new InstaMedia();
            media.Pk = "1";
            medias.Add(media);
            var success = receiver.RemoveExtraMedia(medias, 0, 0);
            var unsuccess = receiver.RemoveExtraMedia(null, 0, 0);
            Assert.AreEqual(success.Count, 0);
            Assert.AreEqual(unsuccess, null);
        }
    }
}