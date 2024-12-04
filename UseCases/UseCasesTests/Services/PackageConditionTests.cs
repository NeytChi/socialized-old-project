using NUnit;
using NUnit.Framework;
using Serilog;

using Managment;
using database.context;
using Models.SessionComponents;

namespace UseCases.Services.Tests
{
    [TestFixture]
    public class PackageConditionTests
    {
        Context context;
        PackageCondition condition;
        public PackageConditionTests()
        {
            this.context = MockingContextTests.GetContext();
            this.condition = new PackageCondition(context, new LoggerConfiguration().CreateLogger());
        }
        [Test]
        public void CreateFreeAccess()
        {
            var user = MockingContextTests.CreateUser();
            Assert.AreEqual(condition.CreateFreeAccess(user.userId).userId, user.userId);
        }
        [Test]
        public void SetPackage()
        {
            var user = MockingContextTests.CreateUser();
            condition.SetPackage(user.userId, 1, 1);
            condition.SetPackage(user.userId, 2, 3);
            condition.SetPackage(user.userId, 3, 6);
            condition.SetPackage(user.userId, 4, 12);
            condition.SetPackage(user.userId, 0, 123);
        }
        [Test]
        public void GetPackageById()
        {
            for (int i = 1; i < 4; i++)
                Assert.AreEqual(condition.GetPackageById(i).package_id, i);
            Assert.AreEqual(condition.GetPackageById(8).package_id, 0);
            Assert.AreEqual(condition.GetPackageById(-1).package_id, 0);
            Assert.AreEqual(condition.GetPackageById(0).package_id, 0);
        }
        [Test]
        public void UpdateAccess()
        {
            var user = MockingContextTests.CreateUser();
            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            condition.UpdateAccess(access, condition.GetPackageById(2), 3);
        }
        [Test]
        public void IGAccountsIsTrue()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            condition.CreateFreeAccess(user.userId);
            Assert.AreEqual(condition.IGAccountsIsTrue(user, ref message), true);
        }
        [Test]
        public void IGAccountsIsTrue_With_Exist_Account()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            condition.CreateFreeAccess(user.userId);
            MockingContextTests.CreateSession(user.userId);
            Assert.AreEqual(condition.IGAccountsIsTrue(user, ref message), false);
        }
        [Test]
        public void IGAccountsIsTrue_With_Unlimeted_Package()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            condition.SetPackage(user.userId, 4, 1);
            MockingContextTests.CreateSession(user.userId);
            Assert.AreEqual(condition.IGAccountsIsTrue(user, ref message), true);
        }
        [Test]
        public void IGAccountsIsTrue_With_Third_Package()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            condition.SetPackage(user.userId, 3, 3);
            MockingContextTests.CreateSession(user.userId);
            Assert.AreEqual(condition.IGAccountsIsTrue(user, ref message), true);
        }
        [Test]
        public void PostsIsTrue()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            condition.CreateFreeAccess(user.userId);
            Assert.AreEqual(condition.PostsIsTrue(user.userId, ref message), true);
        }
        [Test]
        public void PostsIsTrue_With_Exist_Posts()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            var session = MockingContextTests.CreateSession(user.userId);
            for (int i = 0; i < 5; i++)
                MockingContextTests.CreateAutoPost(session.accountId, true);

            condition.CreateFreeAccess(user.userId);
            Assert.AreEqual(condition.PostsIsTrue(user.userId, ref message), false);
            MockingContextTests.DeleteAutoPost(session.accountId);
        }
        [Test]
        public void PostsIsTrue_With_Unlimeted_Package()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            var session = MockingContextTests.CreateSession(user.userId);
            for (int i = 0; i < 5; i++)
                MockingContextTests.CreateAutoPost(session.accountId, true);

            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            condition.SetPackage(user.userId, 3, 3);
            
            Assert.AreEqual(condition.PostsIsTrue(user.userId, ref message), true);
            MockingContextTests.DeleteAutoPost(session.accountId);
        }
        [Test]
        public void StoriesIsTrue()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            condition.CreateFreeAccess(user.userId);
            Assert.AreEqual(condition.StoriesIsTrue(user.userId, ref message), true);
        }
        [Test]
        public void StoriesIsTrue_With_Exist_Posts()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            var session = MockingContextTests.CreateSession(user.userId);
            for (int i = 0; i < 5; i++)
                MockingContextTests.CreateAutoPost(session.accountId, false);

            condition.CreateFreeAccess(user.userId);
            Assert.AreEqual(condition.StoriesIsTrue(user.userId, ref message), false);
            MockingContextTests.DeleteAutoPost(session.accountId);
        }
        [Test]
        public void StoriesTrue_With_Unlimeted_Package()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            var session = MockingContextTests.CreateSession(user.userId);
            for (int i = 0; i < 5; i++)
                MockingContextTests.CreateAutoPost(session.accountId, false);

            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            condition.SetPackage(user.userId, 3, 3);
            
            Assert.AreEqual(condition.StoriesIsTrue(user.userId, ref message), true);
            MockingContextTests.DeleteAutoPost(session.accountId);
        }
        [Test]
        public void AnalyticsDays()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            Assert.AreEqual(condition.AnalyticsDays(user.userId, ref message) > 0, true);
        }
        [Test]
        public void AnalyticsDays_All_Days()
        {
            string message = "";
            var user = MockingContextTests.CreateUser();
            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            condition.SetPackage(user.userId, 3, 3);
            Assert.AreEqual(condition.AnalyticsDays(user.userId, ref message), -1);
        }
        [Test]
        public void GetServiceAccess()
        {
            var user = MockingContextTests.CreateUser();
            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            ServiceAccess result = condition.GetServiceAccess(user.userId);
            Assert.AreEqual(result.accessId, access.accessId);
            Assert.AreEqual(result.packageType, access.packageType);
        }
        [Test]
        public void GetServiceAccess_With_Expired_Package()
        {
            var user = MockingContextTests.CreateUser();
            ServiceAccess access = condition.CreateFreeAccess(user.userId);
            condition.SetPackage(user.userId, 3, -3);
            ServiceAccess result = condition.GetServiceAccess(user.userId);
            Assert.AreEqual(result.accessId, access.accessId);
            Assert.AreEqual(result.packageType, 1);
        }
        [Test]
        public void PayForPackage()
        {
            string message = string.Empty;
            // Assert.AreEqual(condition.PayForPackage(0, "", "", ref message), false);
        }
        [Test]
        public void PayForPackage_With_Empty_Nonce()
        {
            string message = string.Empty;
            Assert.AreEqual(condition.PayForPackage(10, "", "", ref message), false);
        }
        [Test]
        public void CalcPackagePrice_Without_Discount()
        {
            PackageAccess package = new PackageAccess() {
                package_id = 1,
                package_price = 20
            };
            Assert.AreEqual(condition.CalcPackagePrice(package, 1), 20);
        }
        [Test]
        public void CalcPackagePrice_Without_Discount_More_Month()
        {
            PackageAccess package = new PackageAccess() {
                package_id = 1,
                package_price = 20
            };
            Assert.AreEqual(condition.CalcPackagePrice(package, 2), 40);
        }
        [Test]
        public void CalcPackagePrice_With_Discount()
        {
            PackageAccess package = new PackageAccess() {
                package_id = 1,
                package_price = 20
            };
            Assert.AreEqual(condition.CalcPackagePrice(package, 3), 54);
        }
        [Test]
        public void GetDiscountByMonth()
        {
            Assert.AreEqual(condition.GetDiscountByMonth(3).discount_month, 3);
            Assert.AreEqual(condition.GetDiscountByMonth(4).discount_month, 3);
            Assert.AreEqual(condition.GetDiscountByMonth(6).discount_month, 6);
            Assert.AreEqual(condition.GetDiscountByMonth(8).discount_month, 6);
        }
    }
}