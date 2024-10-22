using Common;
using Controllers;
using socialized;
using database.context;
using Models.AutoPosting;
using Models.SessionComponents;
using InstagramService;
using InstagramApiSharp.API.Builder;

using Serilog.Core;
using System;
using System.IO;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Managment
{
    public class AutoPostingManager
    {
        private string DOMEN;
        public Logger log;
        public Context context;
        public TaskDataCondition handler;
        public AwsUploader fileManager;
        public SessionManager sessionManager;
        public PackageCondition access;
        public ConverterFiles converter;
        public int availableHashtags;
        public int availableTags;

        public AutoPostingManager(Logger log, Context context)
        {
            var configuration = Program.serverConfiguration();
            this.DOMEN = configuration.GetValue<string>("aws_host_url");
            this.availableHashtags = configuration.GetValue<int>("available_hashtags");
            this.availableTags = configuration.GetValue<int>("available_tags");
            this.log = log;
            this.context = context;
            this.sessionManager = new SessionManager(context);
            this.handler = new TaskDataCondition(log, sessionManager, new SessionStateHandler(context));
            this.fileManager = new AwsUploader(log);
            this.access = new PackageCondition(context, log);
            this.converter = new ConverterFiles(log);
        }
        public bool CreatePost(AutoPostCache cache, ref string message)
        {
            IGAccount account; List<PostFile> postFiles;
            if ((account = sessionManager.GetUsableSession(cache.user_token, cache.session_id)) != null) {
                if (cache.post_type ? access.PostsIsTrue(account.userId, ref message) : 
                        access.StoriesIsTrue(account.userId, ref message)) {    
                    if (CheckAutoPost(cache, ref message)) {
                        if ((postFiles = SavePostFiles(cache.files, 1, ref message)) != null) {
                            SaveAutoPost(cache, postFiles);
                            return true;
                        }
                    }
                }
            }
            else 
                message = "Server can't define session.";
            return false;
        }
        public AutoPost SaveAutoPost(AutoPostCache cache, ICollection<PostFile> postFiles)
        {
            int timezone = cache.timezone > 0 ? -cache.timezone : cache.timezone * -1;
            AutoPost post = new AutoPost() {
                sessionId = cache.session_id,
                postType = cache.post_type,
                createdAt = DateTimeOffset.UtcNow,
                executeAt = cache.execute_at.AddHours(timezone),
                autoDelete = cache.auto_delete != null ? (bool)cache.auto_delete : false,
                deleteAfter = cache.auto_delete == true 
                    ? cache.delete_after.AddHours(timezone) : cache.delete_after,
                postLocation = HttpUtility.UrlDecode(cache.location),
                postDescription = HttpUtility.UrlDecode(cache.description),
                postComment = HttpUtility.UrlDecode(cache.comment),
                timezone = cache.timezone,
                categoryId = cache.category_id,
                files = postFiles
            };
            context.AutoPosts.Add(post);
            context.SaveChanges();
            log.Information("Create new auto post, id -> " + post.postId);
            return post;
        }
        public List<PostFile> SavePostFiles(ICollection<IFormFile> files, sbyte startOrder, ref string message)
        {
            List<PostFile> postFiles = new List<PostFile>();   
            
            foreach(IFormFile file in files) {
                PostFile post = new PostFile();
                post.fileType = file.ContentType.Contains("video");
                post.fileOrder = startOrder++;
                post.createdAt = DateTimeOffset.UtcNow;
                if (post.fileType) {
                    if (!CreateVideoFile(ref post, file, ref message))
                        return null;
                }
                else {
                    if (!CreateImageFile(ref post, file, ref message))
                        return null;
                }
                postFiles.Add(post);
            }
            return postFiles;

        }
        public bool CreateVideoFile(ref PostFile post, IFormFile file, ref string message)
        {
            Stream stream; string pathFile;

            if ((pathFile = converter.ConvertVideo(file)) != null) {
                stream = File.OpenRead(pathFile + ".mp4");
                if (File.Exists(pathFile))
                    File.Delete(pathFile);
                post.filePath = fileManager.SaveFile(stream, "auto-posts");
                stream = converter.GetVideoThumbnail(pathFile + ".mp4");
                post.videoThumbnail = fileManager.SaveFile(stream, "auto-posts");
                File.Delete(pathFile + ".mp4");
                return true;
            }
            message = "Unknow video format defined.";
            return false;
        }
        public bool CreateImageFile(ref PostFile post, IFormFile file, ref string message)
        {
            Stream stream;

            if ((stream = converter.ConvertImage(file)) != null) {
                post.filePath = fileManager.SaveFile(stream, "auto-posts");
                return true;
            }
            message = "Unknow image format defined.";
            return false;
        }
        public bool CheckAutoPost(AutoPostCache cache, ref string message)
        {
            if (CheckFiles(cache.files, ref message)) {
                if (cache.post_type)
                    return CheckPost(cache, ref message);
                else
                    return CheckStories(cache, ref message);
            }
            return false;
        }
        public bool CheckStories(AutoPostCache cache, ref string message)
        {
            if (CheckExecuteTime(cache.execute_at, cache.timezone, ref message))
                if (CheckDeleteAfter(cache.auto_delete, cache.delete_after, cache.execute_at, ref message))
                    if (CheckToUpdateTimezone(cache.timezone, ref message))
                        if (CheckCategory(cache.session_id, cache.category_id, ref message))
                            return true;
            return false;
        }
        public bool CheckPost(AutoPostCache cache, ref string message)
        {
            if (CheckStories(cache, ref message))
                if (CheckToUpdateDescription(cache.description, ref message))
                    if(CheckToUpdateComment(cache.comment, ref message))
                        if (CheckToUpdateLocation(cache.location, cache.session_id, ref message))
                            return true;
            return false;
        }
        public bool CheckExecuteTime(DateTime executeAt, int timezone, ref string message)
        {
            timezone = timezone > 0 ? -timezone : timezone * -1;
            if (executeAt.AddHours(timezone) > DateTimeOffset.UtcNow)
                return true;
            message = "Auto post can't be execute in past.";
            return false;
        }
        public bool CheckLocation(string location, long sessionId, ref string message)
        {
            if (!string.IsNullOrEmpty(location)) {
                Session session = sessionManager.LoadSession(sessionId);
                if (session != null)
                    return handler.CheckExistLocation(ref session, location, 0, 0, ref message);
                else
                    message = "Server can't define session.";
            }
            else
                message = "Location can't be null or empty.";
            log.Warning(message);
            return false;
        }
        public bool CheckDescription(string description, ref string message)
        {
            if (!string.IsNullOrEmpty(description)) {
                if (description.Length < 2200) {
                    if (UploadedTextIsTrue(description, ref message))
                        return true;
                }
                else
                    message = "Description can't be more 2200 characters.";
            }
            else
                message = "Description is null or empty.";
            log.Warning(message);
            return false;
        }
        public bool CheckComment(string comment, ref string message)
        {
            if (!string.IsNullOrEmpty(comment)) {
                if (comment.Length < 256) {
                    if (UploadedTextIsTrue(comment, ref message))
                        return true;
                }
                else
                    message = "Comment can't be more 256 characters.";
            }
            else
                message = "comment is null or empty.";
            log.Warning(message);
            return false;
        }
        public bool CheckFiles(ICollection<IFormFile> files, ref string message)
        {
            if (files != null) {
                if (files.Count > 0 && files.Count <= 10) {
                    foreach (IFormFile file in files) {
                        if (!CheckFileType(file.ContentType, ref message))
                            return false;
                    }
                    return true;
                }
                else
                    message = "Required count of files from 1 to 10.";
            }
            else
                message = "No post files here.";
            log.Warning(message);
            return false;
        }
        public bool CheckFileType(string contentType, ref string message)
        {
            if (contentType.Contains("video"))
                return true;
            else if (contentType.Contains("image") || contentType.Contains("application/octet-stream"))
                return true;
            else
                message = "File has incorrect format. Required format -> 'image' or 'video'.";
            return false;
        }
        public bool CheckDeleteAfter(bool? autoDelete, DateTime deleteAfter, DateTime executeAt, ref string message)
        {
            if (autoDelete != null) {
                if (autoDelete == false) {
                    log.Information("Auto delete option is off.");
                    return true;
                }
                else {
                    if (deleteAfter > executeAt)
                        return true;
                    else
                        message = "Delete after option can't be less that execute at.";
                }
            }
            else
                message = "Option 'auto_delete' is null.";
            return false;
        }
        public bool CheckCategory(long accountId, long categoryId, ref string message)
        {
            if (categoryId != 0) {
                Category category = context.Categories.Where(c => c.accountId == accountId
                    && c.categoryId == categoryId
                    && !c.categoryDeleted).FirstOrDefault();
                if (category != null)
                    return true;
                message = "Server can't define category by id.";
                return false;
            }
            return true;
        }
        public bool StartStop(AutoPostCache cache, ref string message)
        {
            AutoPost post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null) {
                if (post.postStopped)
                    post.postStopped = false;
                else
                    post.postStopped = true;
                context.AutoPosts.Update(post);
                context.SaveChanges();
                return true;
            }
            return false;
        }
        public AutoPost GetNonDeletedPost(string userToken, long postId, ref string message)
        {
            AutoPost post = (from p in context.AutoPosts
                join s in context.IGAccounts on p.sessionId equals s.accountId
                join u in context.Users on s.userId equals u.userId
            where u.userToken == userToken
                && p.postId == postId
                && p.postDeleted == false 
            select p).FirstOrDefault();
            if (post == null)
                message = "Server can't define post.";
            return post;
        }
        public AutoPost GetNonExecutedPost(string userToken, long postId, ref string message)
        {
            AutoPost post = (from p in context.AutoPosts
                join s in context.IGAccounts on p.sessionId equals s.accountId
                join u in context.Users on s.userId equals u.userId
            where u.userToken == userToken
                && p.postId == postId
                && p.postDeleted == false 
                && p.postAutoDeleted == false
                && p.postExecuted == false
            select p).FirstOrDefault();
            if (post == null)
                message = "Server can't define post by id -> " + postId;
            return post;
        }
        public dynamic GetByCategory(AutoPostCache cache, ref string message)
        {
            switch(cache.category) {
                case 1: return GetPosts(cache, false, false);
                case 2: return GetPosts(cache, true, false);
                case 3: return GetPosts(cache, true, true);
                case 4: return GetUnpublishedPosts(cache);
                default: log.Warning(message = "Server can't define category of posts.");
                    return null;
            }
        }
        public dynamic GetPosts(AutoPostCache cache, bool postExecuted, bool postAutoDeleted)
        {
            log.Information("Get auto posts for ig account id -> " + cache.session_id);
            return (from p in context.AutoPosts
                join s in context.IGAccounts on p.sessionId equals s.accountId
                join u in context.Users on s.userId equals u.userId
                join f in context.PostFiles on p.postId equals f.postId into files
            where u.userToken == cache.user_token
                && s.accountId == cache.session_id
                && p.postExecuted == postExecuted
                && p.postDeleted == false 
                && p.postAutoDeleted == postAutoDeleted
                && p.executeAt > cache.@from
                && p.executeAt < cache.to
            orderby p.postId descending
            select new {
                post_id = p.postId,
                post_type = p.postType,
                created_at = p.createdAt,
                execute_at = p.executeAt.AddHours(p.timezone),
                auto_delete = p.autoDelete,
                delete_after = p.autoDelete ? p.deleteAfter.AddHours(p.timezone) : p.deleteAfter,
                post_location = p.postLocation,
                post_description = p.postDescription,
                post_comment = p.postComment,
                timezone = p.timezone,
                category_id = p.categoryId, 
                category_name = p.categoryId == 0 ? "" 
                    : context.Categories.Where(x => x.categoryId == p.categoryId 
                        && !x.categoryDeleted).FirstOrDefault().categoryName ?? "",
                category_color = p.categoryId == 0 ? "" 
                    : context.Categories.Where(x => x.categoryId == p.categoryId 
                        && !x.categoryDeleted).FirstOrDefault().categoryColor ?? "",
                files = GetPostFilesToOutput(files)
            }).Skip(cache.next * cache.count).Take(cache.count).ToList();
        }
        public List<PostFile> GetNonDeleteFiles(long postId)
        {
            return context.PostFiles.Where(f 
                => f.postId == postId
                && f.fileDeleted == false)
            .OrderBy(f => f.fileOrder).ToList();
        }
        public dynamic GetPostFilesToOutput(IEnumerable<PostFile> files)
        {
            List<dynamic> data = new List<dynamic>();
            foreach (PostFile file in files)
                data.Add(new {
                    file_id = file.fileId,
                    file_order = file.fileOrder,
                    file_url = DOMEN + file.filePath,
                    created_at = file.createdAt,
                    file_type = file.fileType
                });
            return data;
        }
        public bool UpdateAutoPost(AutoPostCache cache, ref string message)
        {
            AutoPost post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null) {
                if (post.postType)
                    return UpdatePost(post, cache, ref message);
                else
                    return UpdateStories(post, cache, ref message);
            }
            return false;
        }
        public bool UpdatePost(AutoPost post, AutoPostCache cache, ref string message)
        {
            cache.session_id = post.sessionId;
            if (CheckToUpdatePost(cache, ref message)) {
                UpdateCommonPost(ref post, cache);
                UpdateDescription(ref post, cache.description);
                UpdateComment(ref post, cache.comment);
                return true;
            }
            return false;
        }
        public bool CheckToUpdateStories(AutoPostCache cache, ref string message)
        {
            if (CheckToUpdateExecuteTime(cache.execute_at, cache.timezone, ref message))
                if (CheckDeleteAfter(cache.auto_delete, cache.delete_after, cache.execute_at, ref message))
                    return true;
            return false;
        }
        public bool CheckToUpdatePost(AutoPostCache cache, ref string message)
        {
            if (CheckToUpdateStories(cache, ref message))
                if (CheckToUpdateDescription(cache.description, ref message))
                    if (CheckToUpdateComment(cache.comment, ref message))
                        if (CheckToUpdateLocation(cache.location, cache.session_id, ref message))
                            if (CheckToUpdateTimezone(cache.timezone, ref message))
                                if (CheckCategory(cache.session_id, cache.category_id, ref message))
                                    return true;
            return false;
        }
        public bool UpdateStories(AutoPost post, AutoPostCache cache, ref string message)
        {
            cache.session_id = post.sessionId;
            if (CheckToUpdateStories(cache, ref message)) {
                UpdateCommonPost(ref post, cache);
                return true;
            }
            return false;
        }
        public void UpdateCommonPost(ref AutoPost post, AutoPostCache cache)
        {
            UpdateExecuteTime(ref post, cache.execute_at, cache.timezone);
            UpdateLocation(ref post, cache.location);
            UpdateDeleteAfter(ref post, cache.auto_delete, cache.delete_after, cache.timezone);        
            UpdateCategory(ref post, cache.category_id);
        }
        public bool CheckToUpdateExecuteTime(DateTime executeTime, int timezone, ref string message)
        {
            if (executeTime != null)
                return CheckExecuteTime(executeTime, timezone, ref message);
            return true;
        }
        public bool CheckToUpdateDeleteAfter(bool? autoDelete, DateTime deleteAfter, DateTime execute_at, int timezone, ref string message)
        {
            if (autoDelete != null && autoDelete == true)
                return CheckDeleteAfter(autoDelete, deleteAfter, execute_at, ref message);
            return true;
        }
        public bool CheckToUpdateLocation(string location, long sessionId, ref string message)
        {
            if (!string.IsNullOrEmpty(location))
                return CheckLocation(location, sessionId, ref message);
            return true;
        }
        public bool CheckToUpdateDescription(string description, ref string message)
        {
            if (!string.IsNullOrEmpty(description))
                return CheckDescription(description, ref message);
            return true;
        }
        public bool CheckToUpdateComment(string comment, ref string message)
        {
            if (!string.IsNullOrEmpty(comment))
                return CheckComment(comment, ref message);
            return true;
        }
        public bool CheckToUpdateTimezone(int timezone, ref string message)
        {
            if (timezone != 0) {
                if (timezone > -14 && timezone < 14)
                    return true;
                else
                    message = "Timezone can't be less -14 & more 14 hours";
                return false;
            }
            return true;
        }
        public void UpdateExecuteTime(ref AutoPost post, DateTime executeTime, int timezone)
        {
            if (executeTime != null) {
                int timezoneDelete = timezone > 0 ? -timezone : timezone * -1;
                post.executeAt = executeTime.AddHours(timezoneDelete);
                post.timezone = timezone;
                context.AutoPosts.Update(post);
                context.SaveChanges();
            }
        }
        public void UpdateLocation(ref AutoPost post, string location)
        {
            if (location != null) {
                post.postLocation = location;
                context.AutoPosts.Update(post);
                context.SaveChanges();
            }
        }
        public void UpdateDeleteAfter(ref AutoPost post, bool? autoDelete, DateTime deleteAfter, int timezone)
        {
            int timezoneDelete = timezone > 0 ? -timezone : timezone * -1;
            post.autoDelete = autoDelete == true ? true : false;
            post.deleteAfter = autoDelete == true ? deleteAfter.AddHours(timezoneDelete) : post.deleteAfter;
            context.AutoPosts.Update(post);
            context.SaveChanges();
        }
        public void UpdateCategory(ref AutoPost post, long categoryId)
        {
            post.categoryId = categoryId;
            context.AutoPosts.Update(post);
            context.SaveChanges();
        }
        public void UpdateDescription(ref AutoPost post, string description)
        {
            post.postDescription = HttpUtility.UrlDecode(description);
            context.AutoPosts.Update(post);
            context.SaveChanges();
        }
        public void UpdateComment(ref AutoPost post, string comment)
        {
            post.postComment = HttpUtility.UrlDecode(comment);
            context.AutoPosts.Update(post);
            context.SaveChanges();
        }
        public bool Delete(AutoPostCache cache, ref string message)
        {
            AutoPost post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null) {
                post.postDeleted = true;
                context.AutoPosts.Update(post);
                context.SaveChanges();
                return true;
            }
            return false;
        }
        public List<AutoPost> GetUnpublishedPosts(AutoPostCache cache)
        {
            return (from p in context.AutoPosts
                join s in context.IGAccounts on p.sessionId equals s.accountId
                join u in context.Users on s.userId equals u.userId
            where u.userToken == cache.user_token
                && s.accountId == cache.session_id
                && p.postExecuted == false
                && p.postDeleted == false 
                && p.postAutoDeleted == false
                && DateTimeOffset.UtcNow > p.executeAt
                && p.executeAt > cache.@from
                && p.executeAt < cache.to
            orderby p.postId descending
            select p).Skip(cache.next * cache.count).Take(cache.count).ToList();
        }
        public List<PostFile> AddFilesToPost(AutoPostCache cache, ref string message)
        {
            List<PostFile> postFiles = null, files; 
            AutoPost post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null) {
                if (CheckFiles(cache.files, ref message)) {
                    files = GetNonDeleteFiles(post.postId);
                    if (files.Count + cache.files.Count <= 10) {
                        if ((postFiles = SavePostFiles(cache.files, (sbyte)(files.Count + 1), ref message)) != null) {
                            foreach (PostFile file in postFiles) {
                                file.postId = post.postId;
                                context.PostFiles.Add(file);
                            }
                            context.SaveChanges();
                        }
                    }
                    else 
                        message = "One post can't contain more that 10 images or videos.";
                }
            }
            return postFiles;
        }
        public bool DeletePostFile(AutoPostCache cache, ref string message)
        {
            AutoPost post; PostFile file;

            if ((post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message)) != null) {
                if ((file = GetNonDeleteFile(cache.file_id, post.postId, ref message)) != null) {
                    file.fileDeleted = true;
                    context.PostFiles.Update(file);
                    context.SaveChanges();
                    List<PostFile> files = GetNonDeleteFiles(post.postId);
                    if (files.Count == 0)
                        Delete(cache, ref message);
                    else
                        ResortFiles(files, file.fileOrder);
                    log.Information("Delete file from auto-post, id -> " + file.fileId);
                    return true;
                }
            }
            return false;
        }
        public void ResortFiles(ICollection<PostFile> files, sbyte deleteOrder)
        {
            foreach (PostFile file in files) {
                if (file.fileOrder > deleteOrder)
                    --file.fileOrder;
            }
            context.PostFiles.UpdateRange(files);
            context.SaveChanges();
        }
        public bool UpdateOrderFile(AutoPostCache cache, ref string message)
        {
            AutoPost post;
            
            if ((post = GetNonExecutedPost(cache.user_token, cache.post_id, ref message)) != null) {
                post.files = GetNonDeleteFiles(post.postId);
                if (FilesIdIsTrue(post.files, cache.files_id, ref message))
                    return ChangeOrderFiles(cache.files_id, post.files); 
            }
            log.Warning(message);
            return false;
        }
        public bool ChangeOrderFiles(List<long> filesId, ICollection<PostFile> files)
        {
            foreach (PostFile file in files) {
                for (sbyte order = 0; order < files.Count; order++) {
                    if (file.fileId == filesId[order])
                        file.fileOrder = (sbyte)(order + 1);
                }
            }
            context.PostFiles.UpdateRange(files);
            context.SaveChanges();
            return true;
        }
        public bool Recovery(AutoPostCache cache, ref string message)
        {
            AutoPost post; IGAccount account; List<PostFile> files;

            if ((post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message)) != null) {
                cache.session_id = post.sessionId;
                cache.post_type = post.postType;
                account = context.IGAccounts.Where(a => a.accountId == post.sessionId).First();
                if (cache.post_type ? access.PostsIsTrue(account.userId, ref message) : 
                        access.StoriesIsTrue(account.userId, ref message)) {    
                    if (CheckExecuteTime(cache.execute_at, cache.timezone, ref message)) {
                        if (post.postType ? CheckToUpdatePost(cache, ref message) : CheckToUpdateStories(cache, ref message)) {
                            post.files = GetNonDeleteFiles(post.postId);
                            if (FilesIdIsTrue(post.files, cache.files_id, ref message)) {
                                files = CreateDuplicatePostFile(post.files);
                                post.postDeleted = true;
                                context.AutoPosts.Update(post);
                                context.SaveChanges();
                                post = SaveAutoPost(cache, files);
                                ChangeOrderFiles(cache.files_id, post.files);
                                log.Information("Recovery auto post, id -> " + post.postId);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public List<PostFile> CreateDuplicatePostFile(ICollection<PostFile> files)
        {
            List<PostFile> duplicate = new List<PostFile>();
            foreach(PostFile file in files) {
                duplicate.Add(new PostFile(){
                    filePath = file.filePath,
                    fileDeleted = file.fileDeleted,
                    fileOrder = file.fileOrder,
                    fileType = file.fileType,
                    mediaId = file.mediaId,
                    videoThumbnail = file.videoThumbnail,
                    createdAt = file.createdAt
                });
            }
            return duplicate;
        }
        public void UpdatePostToExecute(ref AutoPost post)
        {
            post.postExecuted = false;
            post.postAutoDeleted = false;
            context.AutoPosts.Update(post);
            context.SaveChanges();
        }
        public bool FilesIdIsTrue(ICollection<PostFile> files, List<long> filesId, ref string message)
        {
            if (files.Count == filesId.Count) {
                foreach (PostFile postFile in files) {
                    if (!filesId.Contains(postFile.fileId)) {
                        message = "Array of file id doesn't contain file id -> " + postFile.fileId;
                        return false;
                    }
                }
                return true;
            }
            else
                message = "Count of files doesn't compare with array of file id.";
            return false;
        }
        public PostFile GetNonDeleteFile(long fileId, long postId, ref string message)
        {
            PostFile file = context.PostFiles.Where(f => f.fileId == fileId
                && f.postId == postId
                && f.fileDeleted == false)
                .FirstOrDefault();
            if (file == null)
                message = "Server can't define file.";
            return file;
        }
        public bool UploadedTextIsTrue(string uploadedText, ref string message)
        {
            if (string.IsNullOrEmpty(uploadedText))
                return true;
            if (HashtagsIsTrue(uploadedText, ref message)
                && TagsIsTrue(uploadedText, ref message)) {
                log.Information("Check uploaded text -> true");
                return true;
            }
            log.Information("Check uploaded text -> false");
            return false;
        }
        public bool HashtagsIsTrue(string uploadedText, ref string message)
        {
            int hashtagsCount = 0; bool result = false;
            
            hashtagsCount = Regex.Matches(uploadedText, @"#(\w+)").Cast<Match>().ToArray().Length;
            result = hashtagsCount <= availableHashtags;
            if (!result)
                message = "Hashtags can't be more than " + availableHashtags + ".";
            return result;
        }
        public bool TagsIsTrue(string uploadedText, ref string message)
        {
            int tagsCount = 0; bool result = false;
            
            tagsCount = Regex.Matches(uploadedText, @"@(\w+)").Cast<Match>().ToArray().Length;
            result = tagsCount <= availableTags;
            if (!result)
                message = "Tags can't be more than " + availableTags + ".";
            return result;
        }
    }
}