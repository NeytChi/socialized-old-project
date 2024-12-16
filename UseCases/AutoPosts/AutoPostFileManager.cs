using Core.FileControl;
using Domain.AutoPosting;
using Microsoft.AspNetCore.Http;
using Serilog;
using UseCases.AutoPosts.AutoPostFiles;
using UseCases.AutoPosts.AutoPostFiles.Commands;
using UseCases.Exceptions;

namespace UseCases.AutoPosts
{
    public class AutoPostFileManager : BaseManager
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
        public ICollection<AutoPostFile> SavePostFiles(ICollection<IFormFile> files, sbyte startOrder, ref string message)
        {
            var postFiles = new List<AutoPostFile>();

            foreach (var file in files)
            {
                var post = new AutoPostFile
                {
                    Type = file.ContentType.Contains("video"),
                    Order = startOrder++,
                    CreatedAt = DateTime.UtcNow
                };
                if (post.Type)
                {
                    if (!CreateVideoFile(post, file, ref message))
                    {
                        return null;
                    }
                }
                else
                {
                    if (!CreateImageFile(post, file, ref message))
                    {
                        return null;
                    }
                }
                postFiles.Add(post);
            }
            return postFiles;
        }
        public bool CreateVideoFile(AutoPostFile post, IFormFile file, ref string message)
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
        public bool CreateImageFile(AutoPostFile post, IFormFile file, ref string message)
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
