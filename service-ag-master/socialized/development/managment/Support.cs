using System;
using System.Web;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Serilog.Core;

using Common;
using socialized;
using database.context;
using Models.Common;
using Models.AdminPanel;

namespace Managment
{
    public class Support
    {
        private Context context;
        public Logger log;
        private FileManager fileManager;
        public string fileDomen;
        public Support(Logger log, Context context)
        {
            this.context = context;
            this.log = log;
            this.fileManager = new AwsUploader(log);
            this.fileDomen = Program.serverConfiguration().GetValue<string>("aws_host_url");
        }
        public Appeal CreateAppeal(SupportCache cache, ref string message)
        {
            User user = GetNonDeleteUser(cache.user_token, ref message);
            if (user != null && SubjectIsTrue(cache.appeal_subject, ref message)) {
                Appeal appeal = new Appeal() {
                    userId = user.userId,
                    appealSubject = HttpUtility.UrlDecode(cache.appeal_subject),
                    appealState = 1,
                    createdAt = DateTimeOffset.UtcNow,
                    lastActivity = DateTimeOffset.UtcNow
                };
                context.Appeals.Add(appeal);
                context.SaveChanges();
                log.Information("Create new appeal, id -> " + appeal.appealId);
                return appeal;
            }
            return null;
        }
        public bool SubjectIsTrue(string subject, ref string message)
        {
            if (!string.IsNullOrEmpty(subject)) {
                if (subject.Length > 0 && subject.Length < 255)
                    return true;
                message = "Subject length required more than 0 characters & less that 255.";
            }
            else 
                message = "Subject is null or empty.";
            return false;
        }
        public dynamic[] GetAppealsByUser(string userToken, int since, int count)
        {
            log.Information("Get appeals by user, since -> " + since + " count -> " + count);
            return (from appeal in context.Appeals
            join user in context.Users on appeal.userId equals user.userId
            where user.userToken == userToken
            && user.deleted == false
            orderby appeal.appealState
            orderby appeal.createdAt descending
            select new {
                appeal_id = appeal.appealId,
                appeal_subject = appeal.appealSubject,
                appeal_state = appeal.appealState,
                created_at = appeal.createdAt,
                last_activity = appeal.lastActivity
            })
            .Skip(since * count).Take(count).ToArray();
        }
        public dynamic[] GetAppealsByAdmin(int since, int count)
        {
            log.Information("Get appeals by admin, since -> " + since + " count -> " + count);
            return (from appeal in context.Appeals
            orderby appeal.appealState
            orderby appeal.createdAt descending
            select new {
                appeal_id = appeal.appealId,
                appeal_subject = appeal.appealSubject,
                appeal_state = appeal.appealState,
                created_at = appeal.createdAt,
                last_activity = appeal.lastActivity
            })
            .Skip(since * count).Take(count).ToArray();
        }
        public bool EndAppeal(int appealId, ref string message)
        {
            Appeal appeal = GetAppeal(appealId, ref message);
            if (appeal != null) {
                appeal.appealState = 4;
                context.Appeals.Update(appeal);
                context.SaveChanges();
                log.Information("End appeal, id -> " + appeal.appealId);
                return true;
            }
            return false;
        }
        public Appeal GetAppeal(int appealId, ref string message)
        {
            Appeal appeal = context.Appeals.Where(a => a.appealId == appealId).FirstOrDefault();
            if (appeal == null)
                message = "Unknow appeal id.";
            return appeal;
        }
        public Appeal GetAppeal(int appealId, string userToken, ref string message)
        {
            Appeal appeal = (from a in context.Appeals
            join u in context.Users on a.userId equals u.userId
            where a.appealId == appealId 
                && a.appealState != 4
                && u.userToken == userToken
            select a).FirstOrDefault();
            if (appeal == null)
                message = "Unknow user token or appeal id.";
            return appeal;
        }
        public User GetNonDeleteUser(string userToken, ref string message)
        {
            User user = context.Users.Where(u => u.userToken == userToken
                && !u.deleted).FirstOrDefault();
            if (user == null)
                message = "Unknow user token.";
            return user;
        }
        public AppealMessage SendMessage(SupportCache cache, ref string message)
        {
            Appeal appeal;

            if (cache.admin_id != 0) {
                if ((appeal = GetAppeal(cache.appeal_id, ref message)) != null)
                    UpdateAnsweredAppeal(appeal);
            }
            else
                appeal = GetAppeal(cache.appeal_id, cache.user_token, ref message);
            if (appeal != null && AppealMessageIsTrue(cache, ref message)) {
                AppealMessage appealMessage = new AppealMessage() {
                    appealId = appeal.appealId,
                    adminId = cache.admin_id != 0 ? (int?)cache.admin_id : null,
                    messageText = string.IsNullOrEmpty(cache.appeal_message) ? "" : HttpUtility.UrlDecode(cache.appeal_message),
                    createdAt = DateTimeOffset.UtcNow,
                };
                context.AppealMessages.Add(appealMessage);
                context.SaveChanges();
                appealMessage.files = AddFilesToMessage(cache.files, appealMessage.messageId);
                log.Information("Add new appeal message, id -> " + appealMessage.appealId);
                return appealMessage;
            }
            return null;
        }
        public HashSet<AppealFile> AddFilesToMessage(List<IFormFile> upload, long messageId)
        {
            HashSet<AppealFile> files = new HashSet<AppealFile>();
            if (upload != null) {
                foreach (IFormFile file in upload) {
                    AppealFile saved = new AppealFile();
                    saved.messageId = messageId;
                    saved.relativePath = fileManager.SaveFile(file, "support");
                    files.Add(saved);
                }
                context.AppealFiles.AddRange(files);
                context.SaveChanges();
                log.Information("Save files in support");
            }
            return files;
        }
        public bool AppealMessageIsTrue(SupportCache cache, ref string message)
        {
            if (MessageTextIsTrue(cache.appeal_message, ref message)) {
                AppealFilesIsTrue(ref cache.files);
                return true;
            }
            else if (AppealFilesIsTrue(ref cache.files))
                return true;
            else 
                return false;
        }
        public bool MessageTextIsTrue(string messageText, ref string message)
        {
            if (!string.IsNullOrEmpty(messageText)) {
                if (messageText.Length > 0 && messageText.Length < 2000)
                    return true;
                message = "Message text length required more than 0 characters & less that 2000.";
            }
            else 
                message = "Message text is null or empty.";
            return false;
        }
        public bool AppealFilesIsTrue(ref List<IFormFile> files)
        {
            if (files != null) {
                if (files.Count >= 1) {
                    if (files.Count > 3) {
                        for (int i = 3; i < files.Count; i++)
                            files.RemoveAt(i);
                    }
                    return true;
                }
            }
            return false;
        }
        public dynamic[] GetAppealMessages(int appealId, int since, int count)
        {
            log.Information("Get appeal messages, appeal id -> " + appealId);
            return (from message in context.AppealMessages
            join appeal in context.Appeals on message.appealId equals appeal.appealId
            join user in context.Users on appeal.userId equals user.userId
            join admin in context.Admins on message.adminId equals admin.adminId into admins
            join file in context.AppealFiles on message.messageId equals file.messageId into files
            where appeal.appealId == appealId
            orderby message.messageId descending
            select new {
                message_id = message.messageId,
                message_text = message.messageText,
                created_at = message.createdAt,
                files = files.Select(f => new {
                    file_id = f.fileId,
                    file_url = fileDomen + f.relativePath
                }).ToArray(),
                sender = admins.Count() == 1 ? GetSender(admins) : GetSender(user)
            })
            .Skip(since * count).Take(count).ToArray();
        }
        public dynamic GetSender(dynamic adminOrUser)
        { 
            if (adminOrUser.GetType() == typeof(List<Admin>)) {
                List<Admin> admins = (List<Admin>) adminOrUser;
                Admin admin = admins.First();
                return new {
                    user_admin = false,
                    admin_id = admin.adminId,
                    admin_fullname = admin.adminFullname,
                    admin_role = admin.adminRole
                };
            }
            else if (adminOrUser.GetType() == typeof(User)) {
                User user = (User)adminOrUser;
                return new {
                    user_admin = true,
                    user_id = user.userId,
                    user_fullname = user.userFullName,
                };
            }            
            log.Error("Can't convert sender object in GetSender function.");
            return null;
        }
        public bool UpdateAnsweredAppeal(Appeal appeal)
        {
            if (appeal.appealState == 1 || appeal.appealState == 2) {
                appeal.appealState = 3;
                context.Appeals.Update(appeal);
                context.SaveChanges();
                log.Information("Update appeal to answered, id -> " + appeal.appealId);
                return true;
            }
            return false;
        }
        public bool UpdateReadAppeal(int appealId, ref string message)
        {
            Appeal appeal = GetAppeal(appealId, ref message);
            if (appeal != null) {
                if (appeal.appealState == 1) {
                    appeal.appealState = 2;
                    context.Appeals.Update(appeal);
                    context.SaveChanges();
                    log.Information("Update appeal to read, id -> " + appeal.appealId);
                }
                return true;
            }
            return false;
        }
        public bool GetCacheFromData(IFormCollection data, ref SupportCache cache, ref string message)
        {
            if (data.ContainsKey("data")) {
                JObject json = JsonConvert.DeserializeObject<dynamic>(data["data"]);
                if (json != null) {
                    cache = json.ToObject<SupportCache>();
                    cache.files = data.Files.ToList();
                    return true;
                }
                else
                    message = "Server can't define json in form-data.";
            }
            else 
                message = "Input json data is empty or null";
            return false;
        }
    }
}