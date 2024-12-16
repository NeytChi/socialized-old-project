using Serilog;
using System.Web;
using System.Text.RegularExpressions;
using UseCases.Packages;
using Domain.AutoPosting;
using Microsoft.AspNetCore.Http;
using UseCases.InstagramAccounts;
using UseCases.AutoPosts.Commands;
using Domain.InstagramAccounts;

namespace UseCases.AutoPosts
{
    public class AutoPostManager : BaseManager
    {
        private IAutoPostRepository AutoPostRepository;
        private ICategoryRepository CategoryRepository;
        private IPostFileRepository PostFileRepository;
        private IIGAccountRepository IGAccountRepository;
        private IFileManager FileManager;
        private IFileConverter FileConverter;

        
        public CreateIGAccountManager AccountManager;
        public PackageManager access;
        public TaskDataCondition handler;
        public ConverterFiles converter;
        
        public AutoPostManager(ILogger logger, 
            IAutoPostRepository autoPostRepository,
            ICategoryRepository categoryRepository,
            IPostFileRepository postFileRepository,
            IIGAccountRepository iGAccountRepository,
            IFileManager fileManager,
            IFileConverter fileConverter) : base (logger)
        {
            Logger = logger;
            AutoPostRepository = autoPostRepository;
            CategoryRepository = categoryRepository;
            PostFileRepository = postFileRepository;
            IGAccountRepository = iGAccountRepository;
            FileManager = fileManager;
        }
        public void Create(CreateAutoPostCommand command)
        {
            var account = AccountManager.GetUsableAccount(command.UserToken, command.AccountId);
            
            IGAccount account = (from s in context.IGAccounts
                                 join u in context.Users on s.userId equals u.userId
                                 join st in context.States on s.accountId equals st.accountId
                                 where u.userToken == userToken
                                     && s.accountId == accountId
                                     && s.accountDeleted == false
                                     && st.stateUsable == true
                                 select s).FirstOrDefault();
            if (account == null)
                Logger.Warning("Server can't define usable ig account; id -> " + accountId);
            return account;

            if (account == null)
            {
                message = "Server can't define session.";
            }
            if (command.post_type ? !access.PostsIsTrue(account.userId, ref message)
                : !access.StoriesIsTrue(account.userId, ref message))
            {
                return false;
            }
            if (!CheckAutoPost(command, ref message))
            {
                return false;
            }
            var postFiles = SavePostFiles(command.files, 1, ref message);
            if (postFiles == null)
            {
                return false;
            }
            SaveAutoPost(command, postFiles);
            return true;
        }
        public AutoPost SaveAutoPost(AutoPostCache cache, ICollection<AutoPostFile> postFiles)
        {
            int timezone = cache.timezone > 0 ? -cache.timezone : cache.timezone * -1;
            var post = new AutoPost()
            {
                AccountId = cache.session_id,
                Type = cache.post_type,
                CreatedAt = DateTime.UtcNow,
                ExecuteAt = cache.execute_at.AddHours(timezone),
                AutoDelete = cache.auto_delete != null ? (bool)cache.auto_delete : false,
                DeleteAfter = cache.auto_delete == true ? cache.delete_after.AddHours(timezone) : cache.delete_after,
                Location = HttpUtility.UrlDecode(cache.location),
                Description = HttpUtility.UrlDecode(cache.description),
                Comment = HttpUtility.UrlDecode(cache.comment),
                TimeZone = cache.timezone,
                CategoryId = cache.category_id,
                files = postFiles
            };
            AutoPostRepository.Add(post);
            Logger.Information("Create new auto post, id -> " + post.postId);
            return post;
        }
        
        
        
        
        public bool StartStop(AutoPostCache cache, ref string message)
        {
            var post = GetNonDeletedPost(cache.user_token, cache.post_id, ref message);
            if (post != null)
            {
                if (post.postStopped)
                {
                    post.Stopped = false;
                }
                else
                {
                    post.Stopped = true;
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