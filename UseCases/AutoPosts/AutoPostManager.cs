using Serilog;
using System.Web;
using Domain.AutoPosting;
using Microsoft.AspNetCore.Http;
using UseCases.AutoPosts.Commands;
using Domain.InstagramAccounts;
using UseCases.Exceptions;
using Amazon.Runtime.Internal.Util;

namespace UseCases.AutoPosts
{
    public class AutoPostManager : BaseManager
    {
        private IAutoPostRepository AutoPostRepository;
        private IAutoPostFileManager AutoPostFileManager;
        private ICategoryRepository CategoryRepository;
        private IPostFileRepository PostFileRepository;
        private IIGAccountRepository IGAccountRepository;
        
        
        public AutoPostManager(ILogger logger, 
            IAutoPostRepository autoPostRepository,
            ICategoryRepository categoryRepository,
            IAutoPostFileManager autoPostFileManager,
            IPostFileRepository postFileRepository,
            IIGAccountRepository iGAccountRepository
            ) : base (logger)
        {
            Logger = logger;
            AutoPostFileManager = autoPostFileManager;
            AutoPostRepository = autoPostRepository;
            CategoryRepository = categoryRepository;
            PostFileRepository = postFileRepository;
            IGAccountRepository = iGAccountRepository;
        }
        public void Create(CreateAutoPostCommand command)
        {
            var account = IGAccountRepository.Get(command.UserToken, command.AccountId);
            if (account == null)
            {
                throw new NotFoundException($"Instagram аккаунт не був знайдений по токену користувача={command.UserToken} і id={command.AccountId}.");
            }
            /*
            if (command.AutoPostType ? !access.PostsIsTrue(account.userId, ref message)
                : !access.StoriesIsTrue(account.userId, ref message))
            {
                return false;
            }
            if (!CheckAutoPost(command, ref message))
            {
                return false;
            }
            */
            var postFiles = AutoPostFileManager.Create(command.formFiles, 1);
            SaveAutoPost(command, postFiles);
        }
        public AutoPost SaveAutoPost(AutoPostCommand command, ICollection<AutoPostFile> postFiles)
        {
            int timezone = command.TimeZone > 0 ? -command.TimeZone : command.TimeZone * -1;
            var post = new AutoPost
            {
                AccountId = command.AccountId,
                Type = command.AutoPostType,
                CreatedAt = DateTime.UtcNow,
                ExecuteAt = command.ExecuteAt.AddHours(timezone),
                AutoDelete = command.AutoDelete,
                DeleteAfter = command.DeleteAfter.AddHours(timezone),
                Location = HttpUtility.UrlDecode(command.Location),
                Description = HttpUtility.UrlDecode(command.Description),
                Comment = HttpUtility.UrlDecode(command.Comment),
                TimeZone = command.TimeZone,
                CategoryId = command.CategoryId,
                files = postFiles
            };
            AutoPostRepository.Add(post);
            Logger.Information($"Був створений новий автопост, id={post.Id}.");
            return post;
        }
        public void StartStop(StartStopAutoPostCommand command)
        {
            var post = AutoPostRepository.GetBy(command.UserToken, command.PostId);
            if (post == null)
            {
                throw new NotFoundException($"Сервер не визначив авто-пост по id={command.PostId}.");
            }
            post.Stopped = !post.Stopped;
            AutoPostRepository.Update(post);
            Logger.Information($"Авто пост змінив став запуску з {!post.Stopped} по {post.Stopped}.");
        }
        public ICollection<AutoPost> Get(GetAutoPostsCommand command)
        {
            Logger.Information($"Отримано список авто-постів для Instagram аккаунту, id={command.AccountId}.");
            return AutoPostRepository.GetBy(command);
        }
        public bool UpdateAutoPost(UpdateAutoPostCommand command)
        {
            var post = AutoPostRepository.GetBy(command.UserToken, command.PostId);
            if (post == null)
            {
                throw new NotFoundException($"Сервер не визначив авто-пост по id={command.PostId}.");
            }
            return post.Type ? UpdatePost(post, command) : UpdateStories(post, command);
        }
        public bool UpdatePost(AutoPost post, UpdateAutoPostCommand command)
        { 
            if (CheckToUpdatePost(cache))
            {
                UpdateCommonPost(ref post, cache);
                post.Description = HttpUtility.UrlDecode(description);
                post.Comment = HttpUtility.UrlDecode(command.Comment);
                AutoPostRepository.Update(post);
                return true;
            }
            return false;
        }
        public bool UpdateStories(AutoPost post, UpdateAutoPostCommand command)
        {
            cache.session_id = post.sessionId;
            if (CheckToUpdateStories(cache, ref message))
            {
                UpdateCommonPost(post, command);
                return true;
            }
            return false;
        }
        public void UpdateCommonPost(AutoPost post, UpdateAutoPostCommand command)
        {
            int timezoneDelete = command.TimeZone > 0 ? -command.TimeZone : command.TimeZone * -1;
            post.ExecuteAt = command.ExecuteAt.AddHours(timezoneDelete);
            post.TimeZone = command.TimeZone;
            post.Location = command.Location;
            post.AutoDelete = post.AutoDelete;
            post.DeleteAfter = post.AutoDelete ? post.DeleteAfter.AddHours(timezoneDelete) : post.DeleteAfter;            
            post.CategoryId = command.CategoryId;
            AutoPostRepository.Update(post);
        }
        
        public void Delete(DeleteAutoPostCommand command)
        {
            var post = AutoPostRepository.GetBy(command.UserToken, command.AutoPostId);
            if (post == null)
            {
                throw new NotFoundException($"Сервер не визначив авто-пост по id={command.AutoPostId}.");
            }
            post.Deleted = true;
            AutoPostRepository.Update(post);
            Logger.Information($"Авто пост був видалений id={post.Id}.");
        }
        public ICollection<AutoPostFile> AddFilesToPost(AutoPostCache cache)
        {
            var post = AutoPostRepository.GetBy(userToken, postId, false);
            if (post == null)
            {
                throw new NotFoundException($"Сервер не визначив авто-пост по id={postId}.");
            }
            if (!CheckFiles(cache.files, ref message))
            {
                return null;
            }
            var files = PostFileRepository.GetBy(post.postId, false);
            if (files.Count + cache.files.Count <= 10)
            {
                var postFiles = AutoPostFileManager.Create(cache.files, (sbyte)(files.Count() + 1));
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
        public bool UpdateOrderFile(AutoPostCache command, ref string message)
        {
            var post = AutoPostRepository.GetBy(command.UserToken, command.PostId);

            if (post != null)
            {
                post.files = PostFileRepository.GetBy(post.postId, false);
                if (FilesIdIsTrue(post.files, command.files_id, ref message))
                {
                    return ChangeOrderFiles(command.files_id, post.files);
                }
            }
            Logger.Warning(message);
            return false;
        }
        public bool ChangeOrderFiles(List<long> filesId, ICollection<PostFile> files)
        {
            foreach (var file in files)
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
        public bool Recovery(RecoveryAutoPostCommand command)
        {
            if (!CheckExecuteTime(cache.execute_at, cache.timezone, ref message))
            {
                return false;
            }
            if (!FilesIdIsTrue(post.files, cache.files_id, ref message))
            {
                return false;
            }
            var updateResult = post.postType ? CheckToUpdatePost(cache, ref message) : CheckToUpdateStories(cache, ref message);
            if (!updateResult)
            {
                return false;
            }

            var post = AutoPostRepository.GetByWithUserAndFiles(command.UserToken, command.AutoPostId);
            if (post == null)
            {
                throw new NotFoundException($"Сервер не визначив авто-пост по id={command.AutoPostId}.");
            }
            var account = IGAccountRepository.Get(post.AccountId);

            var accessResult = post.Type ? access.PostsIsTrue(account.UserId) : access.StoriesIsTrue(account.UserId)
            if (!accessResult)
            {
                return false;
            }
           
            post.files = PostFileRepository.GetBy(post.postId, false);
            
            var files = CreateDuplicatePostFile(post.files);
            post.Deleted = true;
            AutoPostRepository.Update(post);
            post = SaveAutoPost(command, files);
            ChangeOrderFiles(cache.files_id, post.files);
            Logger.Information("Recovery auto post, id -> " + post.postId);
            return true;
        }
        public List<AutoPostFile> CreateDuplicatePostFile(ICollection<AutoPostFile> files)
        {
            var duplicate = new List<AutoPostFile>();
            foreach (var file in files)
            {
                duplicate.Add(new AutoPostFile()
                {
                    Path = file.Path,
                    IsDeleted = file.IsDeleted,
                    Order = file.Order,
                    Type = file.Type,
                    MediaId = file.MediaId,
                    VideoThumbnail = file.VideoThumbnail,
                    CreatedAt = file.CreatedAt
                });
            }
            return duplicate;
        }
        public void UpdatePostToExecute(AutoPost post)
        {
            post.postExecuted = false;
            post.postAutoDeleted = false;
            AutoPostRepository.Update(post);
        }
        
        /*
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
        */
    }
}