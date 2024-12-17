using Serilog;
using System.Web;
using Domain.AutoPosting;
using UseCases.AutoPosts.Commands;
using Domain.InstagramAccounts;
using UseCases.Exceptions;
using UseCases.AutoPosts.AutoPostFiles;

namespace UseCases.AutoPosts
{
    public class AutoPostManager : BaseManager
    {
        private IIGAccountRepository IGAccountRepository;
        private IAutoPostRepository AutoPostRepository;
        private IAutoPostFileManager AutoPostFileManager;
        private ICategoryRepository CategoryRepository;
        private IAutoPostFileManager AutoPostFilesManager;
        private AutoPostCondition AutoPostCondition;
        
        
        public AutoPostManager(ILogger logger, 
            IAutoPostRepository autoPostRepository,
            ICategoryRepository categoryRepository,
            IAutoPostFileManager autoPostFileManager,
            IIGAccountRepository iGAccountRepository) : base (logger)
        {
            Logger = logger;
            AutoPostCondition = new AutoPostCondition(logger);
            AutoPostFileManager = autoPostFileManager;
            AutoPostRepository = autoPostRepository;
            CategoryRepository = categoryRepository;
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
            var postFiles = AutoPostFileManager.Create(command.Files, 1);
            Save(command, postFiles);
        }
        public AutoPost Save(AutoPostCommand command, ICollection<AutoPostFile> postFiles)
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
        public ICollection<AutoPost> Get(GetAutoPostsCommand command)
        {
            Logger.Information($"Отримано список авто-постів для Instagram аккаунту, id={command.AccountId}.");
            return AutoPostRepository.GetBy(command);
        }
        public void Update(UpdateAutoPostCommand command)
        {
            var post = AutoPostRepository.GetBy(command.UserToken, command.PostId);
            if (post == null)
            {
                throw new NotFoundException($"Сервер не визначив авто-пост по id={command.PostId}.");
            }

            /// System validation - not user validation

            /// if (CheckToUpdatePost(cache) && CheckToUpdateStories(cache))
            {
            ///    throw new SystemValidationException("При зміні автопосту виникли системні помилка від даних, котрі були відправленні.");
            }
            
            int timezoneDelete = command.TimeZone > 0 ? -command.TimeZone : command.TimeZone * -1;
            post.ExecuteAt = command.ExecuteAt.AddHours(timezoneDelete);
            post.TimeZone = command.TimeZone;
            post.Location = command.Location;
            post.AutoDelete = post.AutoDelete;
            post.DeleteAfter = post.AutoDelete ? post.DeleteAfter.AddHours(timezoneDelete) : post.DeleteAfter;
            post.CategoryId = command.CategoryId;
            post.Description = HttpUtility.UrlDecode(command.Description);
            post.Comment = HttpUtility.UrlDecode(command.Comment);
            AutoPostFileManager.Update(command.Files, post.files);
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
        
        /*
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
            post = Save(command, files);
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
        */
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