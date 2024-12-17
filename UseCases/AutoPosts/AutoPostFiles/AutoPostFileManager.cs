using Core.FileControl;
using Domain.AutoPosting;
using Microsoft.AspNetCore.Http;
using Serilog;
using UseCases.AutoPosts.AutoPostFiles.Commands;
using UseCases.Exceptions;

namespace UseCases.AutoPosts.AutoPostFiles
{
    public interface IAutoPostFileManager
    {
        ICollection<AutoPostFile> Create(ICollection<CreateAutoPostFileCommand> files, sbyte startOrder);
        void Update(ICollection<UpdateAutoPostFileCommand> commandFiles, ICollection<AutoPostFile> autoPost);
    }
    public class AutoPostFileManager : BaseManager, IAutoPostFileManager
    {
        private IAutoPostRepository AutoPostRepository;
        private IAutoPostFileRepository AutoPostFileRepository;
        private IFileConverter FileConverter;
        private IFileManager FileManager;

        public AutoPostFileManager(ILogger logger,
            IAutoPostRepository autoPostRepository,
            IFileManager fileManager,
            IAutoPostFileRepository autoPostFileRepository,
            IFileConverter fileConverter) : base(logger)
        {
            AutoPostRepository = autoPostRepository;
            AutoPostFileRepository = autoPostFileRepository;
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
                    if (!CreateVideoFile(post, file.FormFile))
                    {
                        throw new IgAccountException("Сервер не зміг зберегти відео файл.");
                    }
                }
                else
                {
                    if (!CreateImageFile(post, file.FormFile))
                    {
                        throw new IgAccountException("Сервер не зміг зберегти зображення.");
                    }
                }
                postFiles.Add(post);
            }
            return postFiles;
        }
        public bool CreateVideoFile(AutoPostFile post, IFormFile file)
        {
            string pathFile = converter.ConvertVideo(file);

            if (pathFile != null)
            {
                var stream = File.OpenRead(pathFile + ".mp4");
                if (File.Exists(pathFile))
                {
                    File.Delete(pathFile);
                }
                post.Path = FileManager.SaveFile(stream, "auto-posts");
                stream = converter.GetVideoThumbnail(pathFile + ".mp4");
                post.VideoThumbnail = FileManager.SaveFile(stream, "auto-posts");
                File.Delete(pathFile + ".mp4");
                return true;
            }
            message = "Unknow video format defined.";
            return false;
        }
        public bool CreateImageFile(AutoPostFile post, IFormFile file)
        {
            var stream = converter.ConvertImage(file);

            if (stream != null)
            {
                post.Path = FileManager.SaveFile(stream, "auto-posts");
                return true;
            }
            message = "Unknow image format defined.";
            return false;
        }
    }
}
