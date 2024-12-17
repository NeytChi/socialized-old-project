using Core.FileControl;
using Domain.AutoPosting;
using Serilog;
using UseCases.AutoPosts.AutoPostFiles.Commands;
using UseCases.Exceptions;

namespace UseCases.AutoPosts.AutoPostFiles
{
    public interface IAutoPostFileManager
    {
        ICollection<AutoPostFile> Create(ICollection<CreateAutoPostFileCommand> files, sbyte startOrder);
        void Update(ICollection<UpdateAutoPostFileCommand> commandFiles, ICollection<AutoPostFile> autoPost);
        void Delete(DeleteAutoPostFileCommand command);
    }
    public class AutoPostFileManager : BaseManager, IAutoPostFileManager
    {
        private IAutoPostRepository AutoPostRepository;
        private IAutoPostFileRepository AutoPostFileRepository;
        private IAutoPostFileSave AutoPostFileSave;
       
        public AutoPostFileManager(ILogger logger,
            IAutoPostRepository autoPostRepository,
            IFileManager fileManager,
            IAutoPostFileRepository autoPostFileRepository,
            IAutoPostFileSave autoPostFileSave) : base(logger)
        {
            AutoPostRepository = autoPostRepository;
            AutoPostFileRepository = autoPostFileRepository;
            AutoPostFileSave = autoPostFileSave;
        }
        public ICollection<AutoPostFile> AddRange(AddRangeAutoPostFileCommand command)
        {
            var post = AutoPostRepository.GetByWithUserAndFiles(command.UserToken, command.AutoPostId);
            if (post == null)
            {
                throw new NotFoundException($"Сервер не визначив авто-пост по id={command.AutoPostId}.");
            }
            //if (!CheckFiles(cache.files, ref message))
            {
            //    return null;
            }
            if ((post.files.Count() + command.Files.Count) > 10)
            {
                throw new SystemException("Для автопосту дозволено лише 10 файлів.");
            }
            var postFiles = Create(command.Files, (sbyte)(post.files.Count() + 1));
            foreach (var file in postFiles)
            {
                file.PostId = post.Id;
            }
            AutoPostFileRepository.Create(postFiles);
            return postFiles;
        }
        public void Update(ICollection<UpdateAutoPostFileCommand> commandFiles, ICollection<AutoPostFile> autoPost)
        {
            foreach (var file in commandFiles)
            {
                var exist = autoPost.Where(f => f.Id == file.Id).FirstOrDefault();
                if (exist == null)
                {
                    throw new SystemValidationException("Відправлені file id не відповідають file id на сервері.");
                }
                else
                {
                    exist.Order = file.Order;
                }
            }
        }
        public void Delete(DeleteAutoPostFileCommand command)
        {
            var post = AutoPostRepository.GetByWithFiles(command.AutoPostId);
            if (post == null)
            {
                throw new NotFoundException($"Сервер не визначив файл по id={command.AutoPostId} для видалення.");
            }
            if (post.files.Count == 1)
            {
                post.Deleted = true;
                AutoPostRepository.Update(post);
                Logger.Information($"Авто пост був видалений id={post.Id}, тому що були видалені всі файли.");
            }
            else
            {
                var file = post.files.Where(f => f.Id == command.AutoPostId).First();
                file.IsDeleted = true;
                foreach (var oldFile in post.files)
                {
                    if (oldFile.Order > file.Order)
                    {
                        --oldFile.Order;
                    }
                }
                AutoPostFileRepository.Update(file);
                AutoPostFileRepository.Update(post.files);
                Logger.Information($"Файл був видалений з автопосту, файл id={file.Id}.");
            }
        }
        public ICollection<AutoPostFile> Create(ICollection<CreateAutoPostFileCommand> files, sbyte startOrder)
        {
            var postFiles = new List<AutoPostFile>();

            foreach (var file in files)
            {
                var post = new AutoPostFile
                {
                    Type = file.FormFile.ContentType.Contains("video"),
                    Order = startOrder++,
                    CreatedAt = DateTime.UtcNow
                };
                if (post.Type)
                {
                    if (!AutoPostFileSave.CreateVideoFile(post, file.FormFile))
                    {
                        throw new IgAccountException("Сервер не зміг зберегти відео файл.");
                    }
                }
                else
                {
                    if (!AutoPostFileSave.CreateImageFile(post, file.FormFile))
                    {
                        throw new IgAccountException("Сервер не зміг зберегти зображення.");
                    }
                }
                postFiles.Add(post);
            }
            return postFiles;
        }
    }
}
