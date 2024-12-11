using Serilog;
using System.Web;
using System.Text.RegularExpressions;
using UseCases.Packages;
using Domain.AutoPosting;
using Microsoft.AspNetCore.Http;

namespace UseCases.AutoPosts
{
    public class AutoPostingManager
    {
        private ILogger Logger;
        private IAutoPostRepository AutoPostRepository;
        private ICategoryRepository CategoryRepository;
        private IPostFileRepository PostFileRepository;
        private IIGAccountRepository IGAccountRepository;
        private IFileManager FileManager;
        private IFileConverter FileConverter;

        private string DOMEN;
        public int availableHashtags;
        public int availableTags;
        
        public SessionManager sessionManager;
        public PackageCondition access;
        public TaskDataCondition handler;
        public ConverterFiles converter;
        
        public AutoPostingManager(ILogger logger, 
            IAutoPostRepository autoPostRepository,
            ICategoryRepository categoryRepository,
            IPostFileRepository postFileRepository,
            IIGAccountRepository iGAccountRepository,
            IFileManager fileManager,
            IFileConverter fileConverter)
        {
            Logger = logger;
            AutoPostRepository = autoPostRepository;
            CategoryRepository = categoryRepository;
            PostFileRepository = postFileRepository;
            IGAccountRepository = iGAccountRepository;
            FileManager = fileManager;


            DOMEN = configuration.GetValue<string>("aws_host_url");
            availableHashtags = configuration.GetValue<int>("available_hashtags");
            availableTags = configuration.GetValue<int>("available_tags");
            sessionManager = new SessionManager(context);
            handler = new TaskDataCondition(log, sessionManager, new SessionStateHandler(context));
            FileManager = new AwsUploader(log);
            access = new PackageCondition(context, log);
            converter = new ConverterFiles(log);
        }
        public bool CreatePost(AutoPostCache cache, ref string message)
        {
            var account = sessionManager.GetUsableSession(cache.user_token, cache.session_id);
            if (account == null)
            {
                message = "Server can't define session.";
            }
            if (cache.post_type ? !access.PostsIsTrue(account.userId, ref message)
                : !access.StoriesIsTrue(account.userId, ref message))
            {
                return false;
            }
            if (!CheckAutoPost(cache, ref message))
            {
                return false;
            }
            var postFiles = SavePostFiles(cache.files, 1, ref message);
            if (postFiles == null)
            {
                return false;
            }
            SaveAutoPost(cache, postFiles);
            return true;
        }
        public AutoPost SaveAutoPost(AutoPostCache cache, ICollection<PostFile> postFiles)
        {
            int timezone = cache.timezone > 0 ? -cache.timezone : cache.timezone * -1;
            var post = new AutoPost()
            {
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
            AutoPostRepository.Add(post);
            Logger.Information("Create new auto post, id -> " + post.postId);
            return post;
        }
        public List<PostFile> SavePostFiles(ICollection<IFormFile> files, sbyte startOrder, ref string message)
        {
            var postFiles = new List<PostFile>();

            foreach (var file in files)
            {
                var post = new PostFile
                {
                    fileType = file.ContentType.Contains("video"),
                    fileOrder = startOrder++,
                    createdAt = DateTimeOffset.UtcNow
                };
                if (post.fileType)
                {
                    if (!CreateVideoFile(ref post, file, ref message))
                    {
                        return null;
                    }
                }
                else
                {
                    if (!CreateImageFile(ref post, file, ref message))
                    {
                        return null;
                    }
                }
                postFiles.Add(post);
            }
            return postFiles;

        }
        public bool CreateVideoFile(ref PostFile post, IFormFile file, ref string message)
        {
            string pathFile = converter.ConvertVideo(file);

            if (pathFile != null)
            {
                var stream = File.OpenRead(pathFile + ".mp4");
                if (File.Exists(pathFile))
                {
                    File.Delete(pathFile);
                }
                post.filePath = FileManager.SaveFile(stream, "auto-posts");
                stream = converter.GetVideoThumbnail(pathFile + ".mp4");
                post.videoThumbnail = FileManager.SaveFile(stream, "auto-posts");
                File.Delete(pathFile + ".mp4");
                return true;
            }
            message = "Unknow video format defined.";
            return false;
        }
        public bool CreateImageFile(ref PostFile post, IFormFile file, ref string message)
        {
            var stream = converter.ConvertImage(file);

            if (stream != null)
            {
                post.filePath = FileManager.SaveFile(stream, "auto-posts");
                return true;
            }
            message = "Unknow image format defined.";
            return false;
        }
        public bool CheckAutoPost(AutoPostCache cache, ref string message)
        {
            if (CheckFiles(cache.files, ref message))
            {
                if (cache.post_type)
                {
                    return CheckPost(cache, ref message);
                }
                else
                {
                    return CheckStories(cache, ref message);
                }
            }
            return false;
        }
        public bool CheckStories(AutoPostCache cache, ref string message)
        {
            if (!CheckExecuteTime(cache.execute_at, cache.timezone, ref message))
            {
                return false;
            }
            if (!CheckDeleteAfter(cache.auto_delete, cache.delete_after, cache.execute_at, ref message))
            {
                return false;
            }
            if (!CheckToUpdateTimezone(cache.timezone, ref message))
            {
                return false;
            }
            if (!CheckCategory(cache.session_id, cache.category_id, ref message))
            {
                return false;
            }
            return true;
        }
        public bool CheckPost(AutoPostCache cache, ref string message)
        {
            if (!CheckStories(cache, ref message))
            {
                return false;
            }
            if (!CheckToUpdateDescription(cache.description, ref message))
            {
                return false;
            }
            if (!CheckToUpdateComment(cache.comment, ref message))
            {
                return false;
            }
            if (!CheckToUpdateLocation(cache.location, cache.session_id, ref message))
            {
                return false;
            }
            return true;
        }
        public bool CheckExecuteTime(DateTime executeAt, int timezone, ref string message)
        {
            timezone = timezone > 0 ? -timezone : timezone * -1;
            if (executeAt.AddHours(timezone) > DateTimeOffset.UtcNow)
            {
                return true;
            }
            message = "Auto post can't be execute in past.";
            return false;
        }
        public bool CheckLocation(string location, long sessionId, ref string message)
        {
            if (!string.IsNullOrEmpty(location))
            {
                var session = sessionManager.LoadSession(sessionId);
                if (session != null)
                {
                    return handler.CheckExistLocation(ref session, location, 0, 0, ref message);
                }
                else
                {
                    message = "Server can't define session.";
                }
            }
            else
            {
                message = "Location can't be null or empty.";
            }
            Logger.Warning(message);
            return false;
        }
        public bool CheckDescription(string description, ref string message)
        {
            if (!string.IsNullOrEmpty(description))
            {
                if (description.Length < 2200)
                {
                    if (UploadedTextIsTrue(description, ref message))
                    {
                        return true;
                    }
                }
                else
                {
                    message = "Description can't be more 2200 characters.";
                }
            }
            else
            {
                message = "Description is null or empty.";
            }
            Logger.Warning(message);
            return false;
        }
        public bool CheckComment(string comment, ref string message)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                if (comment.Length < 256)
                {
                    if (UploadedTextIsTrue(comment, ref message))
                    {
                        return true;
                    }
                }
                else
                {
                    message = "Comment can't be more 256 characters.";
                }
            }
            else
            {
                message = "comment is null or empty.";
            }
            Logger.Warning(message);
            return false;
        }
        public bool CheckFiles(ICollection<IFormFile> files, ref string message)
        {
            if (files != null)
            {
                if (files.Count > 0 && files.Count <= 10)
                {
                    foreach (IFormFile file in files)
                    {
                        if (!CheckFileType(file.ContentType, ref message))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    message = "Required count of files from 1 to 10.";
                }
            }
            else
            {
                message = "No post files here.";
            }
            Logger.Warning(message);
            return false;
        }
        public bool CheckFileType(string contentType, ref string message)
        {
            if (contentType.Contains("video"))
            {
                return true;
            }
            if (contentType.Contains("image") || contentType.Contains("application/octet-stream"))
            {
                return true;
            }
            message = "File has incorrect format. Required format -> 'image' or 'video'.";
            return false;
        }
        public bool CheckDeleteAfter(bool? autoDelete, DateTime deleteAfter, DateTime executeAt, ref string message)
        {
            if (autoDelete != null)
            {
                if (autoDelete == false)
                {
                    Logger.Information("Auto delete option is off.");
                    return true;
                }
                else
                {
                    if (deleteAfter > executeAt)
                    {
                        return true;
                    }
                    else
                    {
                        message = "Delete after option can't be less that execute at.";
                    }
                }
            }
            else
            {
                message = "Option 'auto_delete' is null.";
            }
            return false;
        }
        public bool CheckCategory(long accountId, long categoryId, ref string message)
        {
            if (categoryId != 0)
            {
                var category = CategoryRepository.GetBy(accountId, categoryId, false);
                if (category != null)
                {
                    return true;
                }
                message = "Server can't define category by id.";
                return false;
            }
            return true;
        }
        public bool StartStop(AutoPostCache cache, ref string message)
        {
            var post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null)
            {
                if (post.postStopped)
                {
                    post.postStopped = false;
                }
                else
                {
                    post.postStopped = true;
                }
                AutoPostRepository.Update(post);
                return true;
            }
            return false;
        }
        public AutoPost GetNonDeletedPost(string userToken, long postId, ref string message)
        {
            var post = AutoPostRepository.GetBy(userToken, postId, false);
            if (post == null)
            {
                message = "Server can't define post.";
            }
            return post;
        }
        public AutoPost GetNonExecutedPost(string userToken, long postId, ref string message)
        {
            var post = AutoPostRepository.GetBy(userToken, postId, false, false, false);
            if (post == null)
                message = "Server can't define post by id -> " + postId;
            return post;
        }
        public dynamic GetByCategory(AutoPostCache cache, ref string message)
        {
            switch (cache.category)
            {
                case 1: return GetPosts(cache, false, false);
                case 2: return GetPosts(cache, true, false);
                case 3: return GetPosts(cache, true, true);
                case 4: return GetPosts(cache, false, false);
                default:
                    Logger.Warning(message = "Server can't define category of posts.");
                    return null;
            }
        }
        public dynamic GetPosts(AutoPostCache cache, bool postExecuted, bool postAutoDeleted)
        {
            var command = new GetAutoPostsCommand
            {
                UserToken = cache.user_token,
                SessionId = cache.session_id,
                PostExecuted = postExecuted,
                PostDeleted = false,
                PostAutoDeleted = postAutoDeleted,
                From = cache.from,
                To = cache.to,
                Since = cache.next,
                Count = cache.count
            };
            Logger.Information("Get auto posts for ig account id -> " + cache.session_id);

            return AutoPostRepository.GetBy(command);
        }
        public dynamic GetPostFilesToOutput(IEnumerable<PostFile> files)
        {
            var data = new List<dynamic>();
            foreach (var file in files)
                data.Add(new
                {
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
            var post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null)
            {
                if (post.postType)
                {
                    return UpdatePost(post, cache, ref message);
                }
                else
                {
                    return UpdateStories(post, cache, ref message);
                }
            }
            return false;
        }
        public bool UpdatePost(AutoPost post, AutoPostCache cache, ref string message)
        {
            cache.session_id = post.sessionId;
            if (CheckToUpdatePost(cache, ref message))
            {
                UpdateCommonPost(ref post, cache);
                UpdateDescription(ref post, cache.description);
                UpdateComment(ref post, cache.comment);
                return true;
            }
            return false;
        }
        public bool CheckToUpdateStories(AutoPostCache cache, ref string message)
        {
            if (!CheckToUpdateExecuteTime(cache.execute_at, cache.timezone, ref message))
            {
                return false;
            }
            if (!CheckDeleteAfter(cache.auto_delete, cache.delete_after, cache.execute_at, ref message))
            {
                return false;
            }
            return true;
        }
        public bool CheckToUpdatePost(AutoPostCache cache, ref string message)
        {
            if (!CheckToUpdateStories(cache, ref message))
            {
                return false;
            }
            if (!CheckToUpdateDescription(cache.description, ref message))
            {
                return false;
            }
            if (!CheckToUpdateComment(cache.comment, ref message))
            {
                return false;
            }
            if (!CheckToUpdateLocation(cache.location, cache.session_id, ref message))
            {
                return false;
            }
            if (!CheckToUpdateTimezone(cache.timezone, ref message))
            {
                return false;
            }
            if (!CheckCategory(cache.session_id, cache.category_id, ref message))
            {
                return false;
            }
            return true;
        }
        public bool UpdateStories(AutoPost post, AutoPostCache cache, ref string message)
        {
            cache.session_id = post.sessionId;
            if (CheckToUpdateStories(cache, ref message))
            {
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
            {
                return CheckExecuteTime(executeTime, timezone, ref message);
            }
            return true;
        }
        public bool CheckToUpdateDeleteAfter(bool? autoDelete, DateTime deleteAfter, DateTime execute_at, int timezone, ref string message)
        {
            if (autoDelete != null && autoDelete == true)
            {
                return CheckDeleteAfter(autoDelete, deleteAfter, execute_at, ref message);
            }
            return true;
        }
        public bool CheckToUpdateLocation(string location, long sessionId, ref string message)
        {
            if (!string.IsNullOrEmpty(location))
            {
                return CheckLocation(location, sessionId, ref message);
            }
            return true;
        }
        public bool CheckToUpdateDescription(string description, ref string message)
        {
            if (!string.IsNullOrEmpty(description))
            {
                return CheckDescription(description, ref message);
            }
            return true;
        }
        public bool CheckToUpdateComment(string comment, ref string message)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                return CheckComment(comment, ref message);
            }
            return true;
        }
        public bool CheckToUpdateTimezone(int timezone, ref string message)
        {
            if (timezone != 0)
            {
                if (timezone > -14 && timezone < 14)
                {
                    return true;
                }
                message = "Timezone can't be less -14 & more 14 hours";
                return false;
            }
            return true;
        }
        public void UpdateExecuteTime(ref AutoPost post, DateTime executeTime, int timezone)
        {
            if (executeTime != null)
            {
                int timezoneDelete = timezone > 0 ? -timezone : timezone * -1;
                post.executeAt = executeTime.AddHours(timezoneDelete);
                post.timezone = timezone;
                AutoPostRepository.Update(post);
            }
        }
        public void UpdateLocation(ref AutoPost post, string location)
        {
            if (location != null)
            {
                post.postLocation = location; 
                AutoPostRepository.Update(post);
            }
        }
        public void UpdateDeleteAfter(ref AutoPost post, bool? autoDelete, DateTime deleteAfter, int timezone)
        {
            int timezoneDelete = timezone > 0 ? -timezone : timezone * -1;
            post.autoDelete = autoDelete == true ? true : false;
            post.deleteAfter = autoDelete == true ? deleteAfter.AddHours(timezoneDelete) : post.deleteAfter;
            AutoPostRepository.Update(post);
        }
        public void UpdateCategory(ref AutoPost post, long categoryId)
        {
            post.categoryId = categoryId;
            AutoPostRepository.Update(post);
        }
        public void UpdateDescription(ref AutoPost post, string description)
        {
            post.postDescription = HttpUtility.UrlDecode(description);
            AutoPostRepository.Update(post);
        }
        public void UpdateComment(ref AutoPost post, string comment)
        {
            post.postComment = HttpUtility.UrlDecode(comment);
            AutoPostRepository.Update(post);
        }
        public bool Delete(AutoPostCache cache, ref string message)
        {
            var post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null)
            {
                post.postDeleted = true;
                AutoPostRepository.Update(post);
                return true;
            }
            return false;
        }
        public List<PostFile> AddFilesToPost(AutoPostCache cache, ref string message)
        {
            var post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post == null)
            {
                return null;
            }
            if (!CheckFiles(cache.files, ref message))
            {
                return null;
            }
            var files = PostFileRepository.GetBy(post.postId, false);
            if (files.Count + cache.files.Count <= 10)
            {
                var postFiles = SavePostFiles(cache.files, (sbyte)(files.Count + 1), ref message);
                if (postFiles != null)
                {
                    foreach (var file in postFiles)
                    {
                        file.postId = post.postId;
                        PostFileRepository.Create(file);
                    }
                    return postFiles;
                }
            }
            else
            {
                message = "One post can't contain more that 10 images or videos.";
            }
            return null;
        }
        public bool DeletePostFile(AutoPostCache cache, ref string message)
        {
            var post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null)
            {
                var file = GetNonDeleteFile(cache.file_id, post.postId, ref message);
                if (file != null)
                {
                    file.fileDeleted = true;
                    PostFileRepository.Update(file);
                    var files = PostFileRepository.GetBy(post.postId, false);
                    if (files.Count == 0)
                    {
                        Delete(cache, ref message);
                    }
                    else
                    {
                        ResortFiles(files, file.fileOrder);
                    }
                    Logger.Information("Delete file from auto-post, id -> " + file.fileId);
                    return true;
                }
            }
            return false;
        }
        public void ResortFiles(ICollection<PostFile> files, sbyte deleteOrder)
        {
            foreach (var file in files)
            {
                if (file.fileOrder > deleteOrder)
                {
                    --file.fileOrder;
                }
            }
            PostFileRepository.UpdateRange(files);
        }
        public bool UpdateOrderFile(AutoPostCache cache, ref string message)
        {
            var post = GetNonExecutedPost(cache.user_token, cache.post_id, ref message);

            if (post != null)
            {
                post.files = PostFileRepository.GetBy(post.postId, false);
                if (FilesIdIsTrue(post.files, cache.files_id, ref message))
                {
                    return ChangeOrderFiles(cache.files_id, post.files);
                }
            }
            Logger.Warning(message);
            return false;
        }
        public bool ChangeOrderFiles(List<long> filesId, ICollection<PostFile> files)
        {
            foreach (PostFile file in files)
            {
                for (sbyte order = 0; order < files.Count; order++)
                {
                    if (file.fileId == filesId[order])
                    {
                        file.fileOrder = (sbyte)(order + 1);
                    }
                }
            }
            PostFileRepository.UpdateRange(files);
            return true;
        }
        public bool Recovery(AutoPostCache cache, ref string message)
        {
            AutoPost post; List<PostFile> files;

            if ((post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message)) != null)
            {
                cache.session_id = post.sessionId;
                cache.post_type = post.postType;
                var account = IGAccountRepository.GetBy(post.sessionId);
                if (cache.post_type ? access.PostsIsTrue(account.userId, ref message) :
                        access.StoriesIsTrue(account.userId, ref message))
                {
                    if (CheckExecuteTime(cache.execute_at, cache.timezone, ref message))
                    {
                        if (post.postType ? CheckToUpdatePost(cache, ref message) : CheckToUpdateStories(cache, ref message))
                        {
                            post.files = PostFileRepository.GetBy(post.postId, false);
                            if (FilesIdIsTrue(post.files, cache.files_id, ref message))
                            {
                                files = CreateDuplicatePostFile(post.files);
                                post.postDeleted = true;
                                AutoPostRepository.Update(post);
                                post = SaveAutoPost(cache, files);
                                ChangeOrderFiles(cache.files_id, post.files);
                                Logger.Information("Recovery auto post, id -> " + post.postId);
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
            var duplicate = new List<PostFile>();
            foreach (var file in files)
            {
                duplicate.Add(new PostFile()
                {
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
            AutoPostRepository.Update(post);
        }
        public bool FilesIdIsTrue(ICollection<PostFile> files, List<long> filesId, ref string message)
        {
            if (files.Count == filesId.Count)
            {
                foreach (var postFile in files)
                {
                    if (!filesId.Contains(postFile.fileId))
                    {
                        message = "Array of file id doesn't contain file id -> " + postFile.fileId;
                        return false;
                    }
                }
                return true;
            }
            else
            {
                message = "Count of files doesn't compare with array of file id.";
            }
            return false;
        }
        public PostFile GetNonDeleteFile(long fileId, long postId, ref string message)
        {
            var file = PostFileRepository.GetBy(fileId, postId, false);
            if (file == null)
            {
                message = "Server can't define file.";
            }
            return file;
        }
        public bool UploadedTextIsTrue(string uploadedText, ref string message)
        {
            if (string.IsNullOrEmpty(uploadedText))
            {
                return true;
            }
            if (HashtagsIsTrue(uploadedText, ref message) && TagsIsTrue(uploadedText, ref message))
            {
                Logger.Information("Check uploaded text -> true");
                return true;
            }
            Logger.Information("Check uploaded text -> false");
            return false;
        }
        public bool HashtagsIsTrue(string uploadedText, ref string message)
        {
            int hashtagsCount = 0; bool result = false;

            hashtagsCount = Regex.Matches(uploadedText, @"#(\w+)").Cast<Match>().ToArray().Length;
            result = hashtagsCount <= availableHashtags;
            if (!result)
            {
                message = "Hashtags can't be more than " + availableHashtags + ".";
            }
            return result;
        }
        public bool TagsIsTrue(string uploadedText, ref string message)
        {
            int tagsCount = 0; bool result = false;

            tagsCount = Regex.Matches(uploadedText, @"@(\w+)").Cast<Match>().ToArray().Length;
            result = tagsCount <= availableTags;
            if (!result)
            {
                message = "Tags can't be more than " + availableTags + ".";
            }
            return result;
        }
    }
    public struct AutoPostCache
    {
        public string user_token;
        public long session_id;
        public long post_id;
        public bool post_type;
        public List<IFormFile> files;
        public List<long> files_id;
        public DateTime execute_at;
        public bool? auto_delete;
        public DateTime delete_after;
        public string location;
        public string comment;
        public string description;
        public int category;
        public long category_id;
        public int next;
        public int count;
        public DateTime from;
        public DateTime to;
        public long file_id;
        public sbyte order;
        public int timezone;
    }
}