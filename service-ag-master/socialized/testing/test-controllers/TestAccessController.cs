using NUnit;
using NUnit.Framework;

using Controllers;
using database.context;
using Models.Common;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestAccessController
    {
        public Context context;
        public AccessController controller;
        public TestAccessController()
        {
            this.context = TestMockingContext.GetContext();
            this.controller = new AccessController(context);
        }
        [Test]
        public void AccessPackages()
        {
            Assert.AreEqual(controller.AccessPackages().Value.success, true);
        }
        [Test]
        public void Discounts()
        {
            Assert.AreEqual(controller.Discounts().Value.success, true);
        }
        [Test]
        public void GetClientToken()
        {
            User user = TestMockingContext.CreateUser();
            AccessCache cache = new AccessCache();
            cache.user_token = user.userToken;
            Assert.AreEqual(controller.GetClientToken(cache).Value.success, true);
        }
        [Test]
        public void PackagePay()
        {
            User user = TestMockingContext.CreateUser();
            AccessCache cache = new AccessCache();
            cache.user_token = user.userToken;
            cache.month_count = 5;
            cache.package_id = 2;
            cache.nonce_token = TestMockingContext.values.nonce_token;
            if (TestMockingContext.values.send_request_to_instagram_service)
                Assert.AreEqual(controller.PackagePay(cache).Value.success, true);
        }
    }
}