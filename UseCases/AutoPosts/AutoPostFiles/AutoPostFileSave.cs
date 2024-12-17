using Serilog;
using FfmpegConverter;
using Core.FileControl;
using Domain.AutoPosting;
using Microsoft.AspNetCore.Http;

namespace UseCases.AutoPosts.AutoPostFiles
{
    public interface IAutoPostFileSave
    {
        public bool CreateVideoFile(AutoPostFile post, IFormFile file);
        public bool CreateImageFile(AutoPostFile post, IFormFile file);
    }
    public class AutoPostFileSave : BaseManager, IAutoPostFileSave
    {
        private IFileConverter FileConverter;
        private IFileManager FileManager;

        public AutoPostFileSave(ILogger logger,
            IFileConverter fileConverter,
            IFileManager fileManager) : base(logger)
        {
            FileConverter = fileConverter;
            FileManager = fileManager;
        }
        public bool CreateImageFile(AutoPostFile post, IFormFile file)
        {
            var stream = FileConverter.ConvertImage(file.OpenReadStream(), file.ContentType);

            if (stream == null)
            {
                Logger.Error("Сервер не визначив формат картинки для збереження.");
                return false;
            }
            post.Path = FileManager.SaveFile(stream, "auto-posts");
            return true;
        }
        public bool CreateVideoFile(AutoPostFile post, IFormFile file)
        {
            string pathFile = FileConverter.ConvertVideo(file.OpenReadStream(), file.ContentType);

            if (pathFile == null)
            {
                Logger.Error("Сервер не визначив формат відео для збереження.");
                return false;
            }
            var stream = File.OpenRead(pathFile + ".mp4");
            if (File.Exists(pathFile))
            {
                File.Delete(pathFile);
            }
            post.Path = FileManager.SaveFile(stream, "auto-posts");
            var thumbnail = FileConverter.GetVideoThumbnail(pathFile + ".mp4");
            post.VideoThumbnail = FileManager.SaveFile(thumbnail, "auto-posts");
            File.Delete(pathFile + ".mp4");
            return true;
        }
    }
}
