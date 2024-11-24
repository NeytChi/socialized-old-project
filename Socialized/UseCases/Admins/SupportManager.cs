using Serilog;
using System.Web;
using Newtonsoft.Json;
using Domain.Admins;
using Microsoft.AspNetCore.Http;
using Domain.AutoPosting;

namespace UseCases.Admins
{
    public class SupportManager
    {
        private IAppealRepository AppealRepository;
        private IAppealMessageRepository AppealMessageRepository;
        private ICategoryRepository CategoryRepository;
        private ILogger Logger;
        
        
        public SupportManager(ILogger logger,
            IAppealRepository appealRepository,
            IAppealMessageRepository appealMessageRepository,
            ICategoryRepository categoryRepository)
        {
            Logger = logger;
            AppealRepository = appealRepository;
            AppealMessageRepository = appealMessageRepository;
            CategoryRepository = categoryRepository;
        }
        public Appeal CreateAppeal(SupportCache cache, int userId, ref string message)
        {
            if (SubjectIsTrue(cache.appeal_subject, ref message))
            {
                var appeal = new Appeal()
                {
                    userId = userId,
                    appealSubject = HttpUtility.UrlDecode(cache.appeal_subject),
                    appealState = 1,
                    createdAt = DateTimeOffset.UtcNow,
                    lastActivity = DateTimeOffset.UtcNow
                };
                AppealRepository.Create(appeal);
                Logger.Information("Create new appeal, id -> " + appeal.appealId);
                return appeal;
            }
            return null;
        }
        public bool SubjectIsTrue(string subject, ref string message)
        {
            if (!string.IsNullOrEmpty(subject))
            {
                if (subject.Length > 0 && subject.Length < 255)
                {
                    return true;
                }
                message = "Subject length required more than 0 characters & less that 255.";
            }
            else
            {
                message = "Subject is null or empty.";
            }
            return false;
        }
        public dynamic[] GetAppealsByUser(string userToken, int since, int count)
        {
            Logger.Information("Get appeals by user, since -> " + since + " count -> " + count);
            return AppealRepository.GetAppealsBy(userToken, since, count);
        }
        public dynamic[] GetAppealsByAdmin(int since, int count)
        {
            Logger.Information("Get appeals by admin, since -> " + since + " count -> " + count);
            return AppealRepository.GetAppealsBy(since, count);
        }
        public bool EndAppeal(int appealId, ref string message)
        {
            var appeal = GetAppeal(appealId, ref message);
            if (appeal != null)
            {
                appeal.appealState = 4;
                AppealRepository.Update(appeal);
                Logger.Information("End appeal, id -> " + appeal.appealId);
                return true;
            }
            return false;
        }
        public Appeal GetAppeal(int appealId, ref string message)
        {
            var appeal = AppealRepository.GetBy(appealId);
            if (appeal == null)
            {
                message = "Unknow appeal id.";
            }
            return appeal;
        }
        public Appeal GetAppeal(int appealId, string userToken, ref string message)
        {
            var appeal = AppealRepository.GetBy(appealId, userToken);
            if (appeal == null)
                message = "Unknow user token or appeal id.";
            return appeal;
        }
        public AppealMessage SendMessage(SupportCache cache, ref string message)
        {
            Appeal appeal;

            if (cache.admin_id != 0)
            {
                appeal = GetAppeal(cache.appeal_id, ref message);
                if (appeal != null)
                {
                    UpdateAnsweredAppeal(appeal);
                }
                else
                {
                    // ....
                }
            }
            else
            {
                appeal = GetAppeal(cache.appeal_id, cache.user_token, ref message);
            }
            if (AppealMessageIsTrue(cache, ref message))
            {
                var appealMessage = new AppealMessage()
                {
                    appealId = appeal.appealId,
                    adminId = cache.admin_id,
                    messageText = string.IsNullOrEmpty(cache.appeal_message) ? "" : HttpUtility.UrlDecode(cache.appeal_message),
                    createdAt = DateTimeOffset.UtcNow,
                };
                AppealMessageRepository.Create(appealMessage);
                // appealMessage.files = AddFilesToMessage(cache.files, appealMessage.messageId);
                Logger.Information("Add new appeal message, id -> " + appealMessage.appealId);
                return appealMessage;
            }
            return null;
        }
        public HashSet<AppealFile> AddFilesToMessage(List<IFormFile> upload, long messageId)
        {
            var files = new HashSet<AppealFile>();
            if (upload != null)
            {
                foreach (var file in upload)
                {
                    var saved = new AppealFile();
                    saved.messageId = messageId;
                    // saved.relativePath = fileManager.SaveFile(file, "support");
                    files.Add(saved);
                }
                AppealMessageRepository.AddRange(files);
                Logger.Information("Save files in support");
            }
            return files;
        }
        // public bool AppealMessageIsTrue(SupportCache cache, ref List<IFormFile> files, ref string message)
        public bool AppealMessageIsTrue(SupportCache cache, ref string message)
        {
            if (MessageTextIsTrue(cache.appeal_message, ref message))
            {
                // AppealFilesIsTrue(ref files);
                return true;
            }

            // if (AppealFilesIsTrue(ref files))
            {
                return true;
            }
            return false;
        }
        public bool MessageTextIsTrue(string messageText, ref string message)
        {
            if (!string.IsNullOrEmpty(messageText))
            {
                if (messageText.Length > 0 && messageText.Length < 2000)
                {
                    return true;
                }
                message = "Message text length required more than 0 characters & less that 2000.";
            }
            else
            {
                message = "Message text is null or empty.";
            }
            return false;
        }
        public bool AppealFilesIsTrue(ref List<IFormFile> files)
        {
            if (files != null)
            {
                if (files.Count >= 1)
                {
                    if (files.Count > 3)
                    {
                        for (int i = 3; i < files.Count; i++)
                        {
                            files.RemoveAt(i);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        public dynamic[] GetAppealMessages(int appealId, int since, int count)
        {
            Logger.Information("Get appeal messages, appeal id -> " + appealId);
            return AppealMessageRepository.GetAppealMessages(appealId, since, count);
        }
        public dynamic GetSender(dynamic adminOrUser)
        {
            if (adminOrUser.GetType() == typeof(List<Admin>))
            {
                var admins = (List<Admin>)adminOrUser;
                var admin = admins.First();
                return new
                {
                    user_admin = false,
                    admin_id = admin.adminId,
                    admin_fullname = admin.adminFullname,
                    admin_role = admin.adminRole
                };
            }
            /*
            if (adminOrUser.GetType() == typeof(User))
            {
                var user = (User)adminOrUser;
                return new
                {
                    user_admin = true,
                    user_id = user.userId,
                    user_fullname = user.userFullName,
                };
            }*/
            Logger.Error("Can't convert sender object in GetSender function.");
            return null;
        }
        public bool UpdateAnsweredAppeal(Appeal appeal)
        {
            if (appeal.appealState == 1 || appeal.appealState == 2)
            {
                appeal.appealState = 3;
                AppealRepository.Update(appeal);
                Logger.Information("Update appeal to answered, id -> " + appeal.appealId);
                return true;
            }
            return false;
        }
        public bool UpdateReadAppeal(int appealId, ref string message)
        {
            var appeal = GetAppeal(appealId, ref message);
            if (appeal != null)
            {
                if (appeal.appealState == 1)
                {
                    appeal.appealState = 2;
                    AppealRepository.Update(appeal);
                    Logger.Information("Update appeal to read, id -> " + appeal.appealId);
                }
                return true;
            }
            return false;
        }
        public bool GetCacheFromData(IFormCollection data, 
            SupportCache cache, 
            ref string message)
        {
            if (data.ContainsKey("data"))
            {
                var json = JsonConvert.DeserializeObject<dynamic>(data["data"]);
                if (json != null)
                {
                    cache = json.ToObject<SupportCache>();
                    // cache.files = data.Files.ToList();
                    return true;
                }
                else
                {
                    message = "Server can't define json in form-data.";
                }
            }
            else
            {
                message = "Input json data is empty or null";
            }
            return false;
        }
    }
}