using NUnit.Framework;

using Controllers;
using database.context;
using Models.Common;
using Models.AutoPosting;
using Models.SessionComponents;

namespace Testing.Controllers
{
    [TestFixture]
    public class TestCategoryController
    {
        public Context context;
        public CategoryController controller;
        public IGAccount account;
        public User user;
        public TestCategoryController()
        {
            this.context = TestMockingContext.GetContext();
            this.controller = new CategoryController(context);
            this.user = TestMockingContext.CreateUser();
            this.account = TestMockingContext.CreateSession(user.userId);
        }
        [Test]
        public void CreateCategory()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = account.accountId;
            cache.category_name = "Лето";
            cache.category_color = "#12345";            
            var result = controller.Create(cache);
            Assert.AreEqual(result.Value.success, true);
            Assert.AreEqual(result.Value.data.category.category_id > 0, true);
            Assert.AreEqual(result.Value.data.category.category_name, cache.category_name);
            Assert.AreEqual(result.Value.data.category.category_color, cache.category_color);
        }
        [Test]
        public void CreateCategory_With_Exist_Name()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = account.accountId;
            cache.category_name = "Лето";
            cache.category_color = "#12345";            
            controller.Create(cache);
            Assert.AreEqual(controller.Create(cache).Value.success, false);
        }
        [Test]
        public void CreateCategory_With_Empty_Name()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = account.accountId;
            cache.category_name = "";
            cache.category_color = "#12345";            
            Assert.AreEqual(controller.Create(cache).Value.success, false);
        }
        [Test]
        public void CreateCategory_With_Empty_Color()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = account.accountId;
            cache.category_name = "Лето";
            cache.category_color = "";            
            Assert.AreEqual(controller.Create(cache).Value.success, false);
        }
        [Test]
        public void CreateCategory_With_Long_Color()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = account.accountId;
            cache.category_name = "SummerTimeSummerTimeSummerTime";
            cache.category_color = "";            
            Assert.AreEqual(controller.Create(cache).Value.success, false);
        }
        [Test]
        public void CreateCategory_With_Unknow_Account()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = 0;
            cache.category_name = "Лето";
            cache.category_color = "#12345";
            Assert.AreEqual(controller.Create(cache).Value.success, false);
        }
        [Test]
        public void GetCategories()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = account.accountId;
            cache.category_name = "Spring";
            cache.category_color = "#12345";            
            controller.Create(cache);
            var result = controller.GetCategories(account.accountId, 0, 100);
            Assert.AreEqual(result.Value.success, true);
            foreach(dynamic category in result.Value.data.categories) {
                Assert.AreEqual(category.category_id > 0, true);
                Assert.AreEqual(((string) category.category_name).Length > 0, true);
                Assert.AreEqual(((string) category.category_color).Length > 0, true);
            }
        }
        [Test]
        public void Remove()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = account.accountId;
            cache.category_name = "Autumn";
            cache.category_color = "#12345";
            var result = controller.Create(cache);
            cache.category_id = result.Value.data.category.category_id;
            Assert.AreEqual(controller.Remove(cache).Value.success, true);
        }
        [Test]
        public void RemoveAll()
        {
            CategoryCache cache = new CategoryCache();
            cache.user_token = user.userToken;
            cache.account_id = account.accountId;
            cache.category_name = "Autumn";
            cache.category_color = "#12345";
            var result = controller.Create(cache);
            Assert.AreEqual(controller.RemoveAll(cache).Value.success, true);
        }
        [Test]
        public void GetNonDeleteAccount()
        {
            string message = string.Empty;
            IGAccount result = controller.GetNonDeleteAccount(user.userToken, account.accountId, ref message);
            Assert.AreEqual(result.accountId, account.accountId);
        }
        [Test]
        public void GetNonDeleteAccount_With_Unknow_Token()
        {
            string message = string.Empty;
            IGAccount result = controller.GetNonDeleteAccount("", account.accountId, ref message);
            Assert.AreEqual(result, null);
        }
        [Test]
        public void GetNonDeleteAccount_With_Unknow_Id()
        {
            string message = string.Empty;
            IGAccount result = controller.GetNonDeleteAccount(user.userToken, 0, ref message);
            Assert.AreEqual(result, null);
        }
    }
}