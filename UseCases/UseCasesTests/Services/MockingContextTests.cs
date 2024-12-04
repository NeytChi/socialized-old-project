using System;
using Serilog;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Internal;

using Common;
using Models.Common;
using Models.Lending;
using database.context;
using Models.Statistics;
using Models.AdminPanel;
using Models.AutoPosting;
using Models.GettingSubscribes;
using Models.SessionComponents;
using Managment;

namespace UseCases.Services.Tests
{
    [TestFixture]
    public static class MockingContextTests
    {
        private static Context context;
        public static MockingValues values;
        public static SessionManager manager;
        public static Context GetContext()
        {
            if (context == null) {
                SetUpConfiguration();
                context = new Context(values.use_in_memory_database);
                manager = new SessionManager(context);
            }
            return context;
        }
        public static void SetUpConfiguration()
        {
            string fullPath = "/home/neytchi/project/service-ag/socialized/bin/Debug/netcoreapp2.2/testing.json";
            string testingJson = File.ReadAllText(fullPath);
            JObject json = JsonConvert.DeserializeObject<dynamic>(testingJson);
            values = json.ToObject<MockingValues>();
        }
        public static ProfileCondition val = new ProfileCondition(new LoggerConfiguration().CreateLogger());

        public static AdminCache GetAdminCache()
        {
            AdminCache cache = new AdminCache() {
                admin_email = MockingContextTests.values.admin_email,
                admin_password = MockingContextTests.values.admin_password,
                admin_fullname = MockingContextTests.values.admin_fullname        
            };
            DeleteExistAdmin(cache.admin_email);
            return cache;
        }
        public static Follower CreateFollower()
        {
            string followerEmail = values.follower_email;
            List<Follower> followers = context.Followers.ToList();
            context.Followers.RemoveRange(followers);
            context.SaveChanges();
            Follower follower = new Follower() {
                enableMailing = true,
                createdAt = DateTime.Now,
                followerEmail = followerEmail 
            };
            context.Followers.Add(follower);
            context.SaveChanges();
            return follower;
        }
        public static Follower CreateFollower(string userEmail)
        {
            string followerEmail = userEmail;
            Follower follower = context.Followers.Where(f => f.followerEmail == followerEmail).FirstOrDefault();
            if (follower != null) {
                context.Followers.Remove(follower);
                context.SaveChanges();
            }
            follower = new Follower() {
                enableMailing = true,
                createdAt = DateTime.Now,
                followerEmail = followerEmail 
            };
            context.Followers.Add(follower);
            context.SaveChanges();
            return follower;
        }
        public static Admin CreateAdmin()
        {
            DeleteExistAdmin(values.admin_email);
            Admin admin = new Admin()
            {
                adminEmail = values.admin_email,
                adminPassword = val.HashPassword(values.admin_password),
                passwordToken = val.CreateHash(10),
                adminRole = values.admin_role,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                lastLoginAt = 0,
                recoveryCode = null,
                deleted = false,
            };
            context.Admins.Add(admin);
            context.SaveChanges();
            return admin;
        }
        public static void DeleteExistAdmin(string adminEmail)
        {
            Admin admin = context.Admins.Where(a => a.adminEmail == adminEmail).FirstOrDefault();
            if (admin != null) {
                context.Admins.Remove(admin);
                context.SaveChanges();
            }
        }
        public static User CreateUser(string userEmail)
        {
            DeleteUser();
            User user = new User();
            user.userEmail = userEmail;
            user.userToken = values.user_token;
            user.userPassword = val.HashPassword(values.user_password);
            user.activate = true;
            context.Users.Add(user);
            context.SaveChanges();
            return user;
        }
        public static User CreateUser()
        {
            DeleteUsers();
            User user = new User();
            user.userEmail = values.user_email;
            user.userToken = values.user_token;
            user.userPassword = val.HashPassword(values.user_password);
            user.activate = true;
            user.profile = new Profile();
            user.profile.country = "test";
            context.Users.Add(user);
            context.SaveChanges();
            ServiceAccess access = new ServiceAccess() {
                userId = user.userId,
                packageType = 1,
                available = true
            };
            context.ServiceAccess.Add(access);
            context.SaveChanges();
            return user;
        }
        public static void DeleteUsers()
        {
            List<User> users = context.Users.ToList();
            context.Users.RemoveRange(users);
            context.SaveChanges();
        }
        public static void DeleteUser()
        {
            User user = context.Users.Where(u => u.userEmail == values.user_email).FirstOrDefault();
            if (user != null) {
                context.Users.Remove(user);
                context.SaveChanges();
            }
        }
        public static void DeleteAllSessions()
        {
            List<IGAccount> accounts = context.IGAccounts.ToList();
            context.IGAccounts.RemoveRange(accounts);
            context.SaveChanges();
        }
        public static BusinessAccount BusinessAccountEnviroment()
        {
            User user = CreateUser();
            IGAccount account = CreateSession(user.userId);
            DeleteBusinessAccount(user.userId);
            return MockingContextTests.CreateBusinessAccount(account);
        }
        public static void DeleteBusinessAccount(int userId)
        {
            BusinessAccount account = context.BusinessAccounts.Where(b 
                => b.userId == userId).FirstOrDefault();
            if (account != null) {
                context.BusinessAccounts.Remove(account);
                context.SaveChanges();
            }
        }
        public static BusinessAccount CreateBusinessAccount(User user)
        {
            BusinessAccount businessAccount = new BusinessAccount() {
                userId = user.userId,
                accessToken = values.access_token,
                longLiveAccessToken = values.long_live_access_token,
                longTokenExpiresIn = DateTime.Now.AddDays(60),
                facebookId = values.facebook_id,
                businessAccountId = values.business_account_id,
                createdAt = DateTime.Now,
                tokenCreated = DateTime.Now,
            };
            context.BusinessAccounts.Add(businessAccount);
            context.SaveChanges();
            return businessAccount;
        }
        public static BusinessAccount CreateBusinessAccount(IGAccount account)
        {
            BusinessAccount businessAccount = new BusinessAccount() {
                userId = account.userId,
                accessToken = values.access_token,
                longLiveAccessToken = values.long_live_access_token,
                longTokenExpiresIn = DateTime.Now.AddDays(60),
                facebookId = values.facebook_id,
                businessAccountId = values.business_account_id,
                createdAt = DateTime.Now,
                tokenCreated = DateTime.Now,
                // igAccount = account
            };
            context.BusinessAccounts.Add(businessAccount);
            context.SaveChanges();
            return businessAccount;
        }
        public static TaskGS CreateTask(long sessionId)
        {
            context.RemoveRange(context.TaskGS.ToList());
            context.SaveChanges();
            TaskGS task = new TaskGS()
            {
                taskType = values.task_type,
                taskSubtype = values.task_subtype,
                sessionId = sessionId,
                taskDeleted = false
            };
            context.TaskGS.Add(task);
            context.SaveChanges();
            return task;
        }
        public static TaskData CreateTaskData(long taskId)
        {
            TaskData data = new TaskData()
            {
                taskId = taskId,
                dataNames = values.data_name,
                dataLatitute = values.data_latitute,
                dataLongitute = values.data_longitute,
                dataComment = values.data_comment
            };
            context.TaskData.Add(data);
            context.SaveChanges();
            return data;
        }
        public static IGAccount CreateSession(int userId)
        {
            IGAccount account = new IGAccount() {
                userId = userId,
                sessionSave = manager.Encrypt(File.ReadAllText(values.session_path))
            };
            SessionState state = new SessionState();
            state.stateUsable = true;
            account.State = state;
            account.timeAction = new TimeAction();
            context.IGAccounts.Add(account);
            context.States.Add(state);
            context.SaveChanges();
            return account;
        }
        public static UnitGS CreateUnitGS(long dataId)
        {
            UnitGS unit = new UnitGS()
            {
                dataId = dataId,
                userPk = 1,
                userIsPrivate = false,
                username = values.username,
                commentPk = values.comment_pk,
            };
            context.Units.Add(unit);
            context.SaveChanges();
            return unit;
        }
        public static MediaGS CreateMediaGS(long unitId)
        {
            MediaGS mediaGS = new MediaGS()
            {
                unitId = unitId,
                mediaPk = values.media_pk,
                mediaQueue = 1,
                mediaHandled = false
            };
            context.Medias.Add(mediaGS);
            context.SaveChanges();
            return mediaGS;
        }
        public static TaskFilter CreateFilter(long taskId)
        {
            TaskFilter filter = new TaskFilter();
            filter.taskId = taskId;
            context.TaskFilters.Add(filter);
            context.SaveChanges();
            return filter;
        }
        public static TaskOption CreateOption(long taskId)
        {
            TaskOption option = new TaskOption()
            {
                taskId = taskId,
            };
            context.TaskOptions.Add(option);
            context.SaveChanges();
            return option;
        }
        public static BlogPost CreateBlogPost(int adminId)
        {
            BlogPost post = new BlogPost() {
                adminId = adminId,
                postSubject = MockingContextTests.values.post_subject,
                postHtmlText = MockingContextTests.values.post_htmltext,
                postLanguage = MockingContextTests.values.post_language,
                createdAt = DateTime.Now 
            };
            context.BlogPosts.Add(post);
            context.SaveChanges();
            return post;
        }
        public static Appeal CreateAppeal(int userId)
        {
            Appeal appeal = new Appeal() {
                userId = userId,
                appealSubject = MockingContextTests.values.post_subject,
                appealState = 1,
                createdAt = DateTime.Now,
                lastActivity = DateTime.Now
            };
            context.Appeals.Add(appeal);
            context.SaveChanges();
            return appeal;
        }
        public static AppealMessage CreateAppealMessage(int appealId)
        {
            AppealMessage message = new AppealMessage() {
                appealId = appealId,
                messageText = MockingContextTests.values.post_htmltext,
                createdAt = DateTime.Now,
            };
            context.AppealMessages.Add(message);
            context.SaveChanges();
            return message;
        }
        public static AppealMessage CreateAppealMessage(int appealId, int adminId)
        {
            AppealMessage message = new AppealMessage() {
                appealId = appealId,
                adminId = adminId,
                messageText = MockingContextTests.values.post_htmltext,
                createdAt = DateTime.Now,
            };
            context.AppealMessages.Add(message);
            context.SaveChanges();
            return message;
        }
        public static TaskGS CreateTaskGSEnviroment()
        {
            User user = CreateUser();
            IGAccount account = CreateSession(user.userId);
            account.User = user;
            TaskGS task = CreateTask(account.accountId);
            task.account = account;
            task.taskOption = CreateOption(task.taskId);
            task.taskFilter = CreateFilter(task.taskId);
            task.taskData.Add(CreateTaskData(task.taskId));
            return task;
        }
        public static TaskData CreateTaskDataEnviroment()
        {
            User user = CreateUser();
            IGAccount account = CreateSession(user.userId);
            TaskGS task = CreateTask(account.accountId);
            return CreateTaskData(task.taskId);
        }
        public static UnitGS CreateUnitGSEnviroment()
        {
            User user = CreateUser();
            IGAccount account = CreateSession(user.userId);
            TaskGS task = CreateTask(account.accountId);
            task.taskOption = CreateOption(task.taskId);
            task.taskFilter = CreateFilter(task.taskId);
            TaskData data = CreateTaskData(task.taskId);
            UnitGS unit = CreateUnitGS(data.dataId);
            return unit;
        }
        public static BlogPost CreateBlogPostEnviroment()
        {
            Admin admin = CreateAdmin();
            BlogPost post = CreateBlogPost(admin.adminId);
            post.admin = admin;
            return post;
        }
        public static Appeal CreateAppealEnviroment()
        {
            User user = CreateUser();
            Appeal appeal = CreateAppeal(user.userId);
            appeal.user = user;
            return appeal;
        }
        public static AppealMessage CreateAppealMessageEnviroment()
        {
            User user = CreateUser();
            Appeal appeal = CreateAppeal(user.userId);
            appeal.user = user;
            AppealMessage message = CreateAppealMessage(appeal.appealId);
            message.appeal = appeal;
            return message;
        }
        public static AppealMessage CreateAppealMessageEnviromentWithAdmin()
        {
            User user = CreateUser();
            Admin admin = CreateAdmin();
            Appeal appeal = CreateAppeal(user.userId);
            appeal.user = user;
            AppealMessage message = CreateAppealMessage(appeal.appealId, admin.adminId);
            message.appeal = appeal;
            return message;
        }
        public static MediaGS CreateMediaGSEnviroment()
        {
            User user = CreateUser();
            IGAccount account = CreateSession(user.userId);
            TaskGS task = CreateTask(account.accountId);
            TaskData data = CreateTaskData(task.taskId);
            UnitGS unit = CreateUnitGS(data.dataId);
            MediaGS media = CreateMediaGS(unit.unitId);
            return media;
        }
        public static void DeleteAutoPost(long igAccountId)
        {
            AutoPost[] posts = context.AutoPosts.Where(a => a.sessionId == igAccountId).ToArray();
            context.AutoPosts.RemoveRange(posts);
            context.SaveChanges();
        }
        public static AutoPost CreateAutoPost(long igAccountId, bool postType)
        {
            AutoPost post = new AutoPost() {
                sessionId = igAccountId,
                postType = postType,
                postExecuted = false,
                postDeleted = false,
                postStopped = false,
                postAutoDeleted = false,
                createdAt = DateTime.Now,
                executeAt = DateTime.Now.AddDays(2),
                autoDelete = true,
                deleteAfter = DateTime.Now.AddDays(3),
                postLocation = "London",
                postDescription = "Testing",
                postComment = "Testing"
            };
            post.files.Add(new PostFile());
            context.AutoPosts.Add(post);
            context.SaveChanges();
            return post;
        }
        public static IFormFile CreateFile()
        {
            byte[] fileBytes = File.ReadAllBytes(values.image_path);
            FormFile file = new FormFile(new MemoryStream(fileBytes), 0, 0, "file", "parrot.jpg");
            file.Headers = new HeaderDictionary();
            file.ContentType = "image/jpeg";
            return file;
        }
        public static Category CreateCategory(long accountId)
        {
            Category category = new Category();
            category.accountId = accountId;
            category.categoryName = "Summer";
            category.categoryColor = "RED";
            category.createdAt = DateTimeOffset.UtcNow;
            context.Categories.Add(category);
            context.SaveChanges();
            return category;
        }
    }
    public struct MockingValues
    {
        public string admin_email;
        public string admin_fullname;
        public string admin_password;
        public string admin_role;
        public string data_name;
        public int data_latitute;
        public int data_longitute;
        public string data_comment;
        public string username;
        public string comment_pk;
        public string media_pk;
        public sbyte task_type;
        public sbyte task_subtype;
        public string follower_email;
        public string user_email;
        public string user_password;
        public string user_token;
        public string access_token;
        public string long_live_access_token;
        public string facebook_id;
        public string business_account_id;
        public string ig_media_id;
        public string ig_story_id;
        public string session_path;
        public string image_path;
        public string post_subject;
        public string post_htmltext;
        public int post_language;
        public string nonce_token;
        public bool send_request_to_instagram_service;
        public bool use_in_memory_database;
    }
}
