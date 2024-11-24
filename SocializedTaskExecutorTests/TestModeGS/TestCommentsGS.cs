using Serilog;
using NUnit.Framework;
using database.context;
using InstagramService;
using Instasoft.Testing;
using InstagramApiSharp;
using Models.GettingSubscribes;
using InstagramApiSharp.Classes.Models;
using Managment;
using ngettingsubscribers;
using InstagramApiSharp.API;

namespace Common
{
    [TestFixture]
    public class TestCommentsGS
    { 
        public Context context;
        public InstaCommentList comments;
        public TestCommentsGS()
        {
            context = new Context(true, true);
            TestMockingContext.context = context;
            unit = TestMockingContext.CreateUnitGSEnviroment();
            branch = unit.Data.Task.Branch;
            branch.session = SessionManager.CreateInstance(context).LoadSession(branch.sessionId);
            unit.userPk = branch.session.User.LoggedInUser.Pk;
            unit.username = branch.session.User.UserName;
            branch.currentTask = unit.Data.Task;
            branch.currentTaskData = unit.Data;
            SessionStateHandler handler = new SessionStateHandler(context);
            commentsGS = new CommentsGS(OptionsGS.GetInstance(handler),new LoggerConfiguration().CreateLogger(),handler);
            var medias = InstagramApi.GetInstance().users.GetUserMedia(ref branch.session, 
            branch.session.User.UserName, new PaginationParameters()).Value;
            media = medias[0];
            comments = InstagramApi.GetInstance().comment.GetMediaComments(ref branch.session, media.Pk, new PaginationParameters()).Value;
            unit.commentPk = comments.Comments[0].Pk.ToString();
            branch.currentUnit = unit;
        }
        public CommentsGS commentsGS;
        public TaskBranch branch;
        public UnitGS unit;
        public InstaMedia media;
        
        public long firstUserPk = 19330388085;  /// user - nikachikapika2 
        public long secondUserPk = 21363493760; /// user - nikitatesterchi 

        [Test]
        public void HandleTask()
        {
            branch.currentUnit.commentPk = comments.Comments[2].Pk.ToString();
            branch.currentTask.taskSubtype = 6;
            var success = commentsGS.HandleTask(context, ref branch);
            var unsuccess = commentsGS.HandleTask(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void HandlingComment()
        {
            var success = commentsGS.HandlingComment(context, ref branch);
            var unsuccess = commentsGS.HandlingComment(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void HandlingLike()
        {
            branch.currentUnit.commentPk = comments.Comments[0].Pk.ToString();
            var success = commentsGS.HandlingLike(context, ref branch);
            var unsuccess = commentsGS.HandlingLike(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CheckOptions()
        {
            var success = commentsGS.CheckOptions(context, ref branch);
            var unsuccess = commentsGS.CheckOptions(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void LikeUserComment()
        {   
            branch.currentUnit.commentPk = comments.Comments[1].Pk.ToString();
            var success = commentsGS.LikeUserComment(context, ref branch);
            var unsuccess = commentsGS.LikeUserComment(null, ref branch);
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
        [Test]
        public void CommentMedia()
        {
            var success = commentsGS.CommentMedia(branch, media.Pk, "Cool post!");
            var unsuccess = commentsGS.CommentMedia(null, null, "Cool post");
            Assert.AreEqual(success, true);
            Assert.AreEqual(unsuccess, false);
        }
    }
}