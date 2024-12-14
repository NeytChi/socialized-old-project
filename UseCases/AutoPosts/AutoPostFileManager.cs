using Core.FileControl;
using Domain.AutoPosting;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;

namespace UseCases.AutoPosts
{
    public class AutoPostFileManager : BaseManager
    {
        public AutoPostFileManager(ILogger logger) : base(logger)
        {

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
                post.filePath = FileManager.SaveFile(stream, "auto-posts");
                return true;
            }
            message = "Unknow image format defined.";
            return false;
        }
    }
}
