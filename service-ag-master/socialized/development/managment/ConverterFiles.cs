using System;
using System.IO;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog.Core;

using socialized;


namespace Managment
{
    public class ConverterFiles
    {
        public Logger log;
        public string FFmpegExe;
        public ConverterFiles(Logger log)
        {
            var configuration = Program.serverConfiguration();
            this.log = log;
            this.FFmpegExe = configuration.GetValue<string>("ffmpeg_exe_path");
        }
        public Stream ConvertImage(IFormFile file)
        {
            MemoryStream convertedFile = null;
            string pathFile;
            
            pathFile = Directory.GetCurrentDirectory() + "/" + DateTime.Now.Ticks.ToString();
            using (FileStream stream = new FileStream(pathFile, FileMode.Create))
                file.CopyTo(stream);

            if (ConvertImage(file.ContentType, pathFile)) {
                using (Stream stream = File.Open(pathFile + ".jpg", FileMode.Open)) {
                    convertedFile = new MemoryStream();
                    stream.CopyTo(convertedFile);
                    convertedFile.Seek(0, SeekOrigin.Begin);
                }
                if (File.Exists(pathFile)) File.Delete(pathFile);
                File.Delete(pathFile + ".jpg");
            }
            return convertedFile;
        }
        public string ConvertVideo(IFormFile file)
        {
            string pathFile;
            
            pathFile = Directory.GetCurrentDirectory() + "/" + DateTime.Now.Ticks.ToString();
            using (FileStream stream = new FileStream(pathFile, FileMode.Create))
                file.CopyTo(stream);

            if (ConvertVideo(file.ContentType, pathFile))
                return pathFile;
            File.Delete(pathFile);
            return null;
        }
        // public bool ConvertImage(string contentType, string pathFile)
        // {
        //     string arguments;
            
        //     switch (contentType) {
        //         case "image/tiff": 
        //         case "image/png": 
        //         case "image/gif":
        //             arguments = pathFile + " " +  pathFile + ".jpg";
        //             return ConvertImageMagick(arguments);
        //         case "image/jpeg": 
        //             File.Move(pathFile, pathFile + ".jpg");
        //             return true;
        //         default : 
        //             log.Information("Can't define file type to convert file for auto-posting.");
        //             return false;
        //     }
        // }
        public bool ConvertImage(string contentType, string pathFile)
        {
            string arguments;
            
            switch (contentType) {
                case "application/octet-stream":
                case "image/tiff": 
                case "image/png": 
                case "image/gif":
                    arguments = "-i " + pathFile + " -qscale:v 2 " +  pathFile + ".jpg";
                    return ConvertFFmpeg(arguments);
                case "image/jpeg": 
                    File.Move(pathFile, pathFile + ".jpg");
                    return true;
                default : 
                    log.Information("Can't define file type to convert file for auto-posting.");
                    return false;
            }
        }
        public bool ConvertVideo(string contentType, string pathFile)
        {
            string videoType, arguments;
            
            videoType = contentType.Remove(0, 6);
            switch (videoType) {
                case "x-flv":
                    arguments = "-i " + pathFile + " -c:v libx264 -crf 19 -strict experimental " +  pathFile + ".mp4";
                    return ConvertFFmpeg(arguments);
                case "x-msvideo": 
                    arguments = "-i " + pathFile + " -strict -2 " + pathFile + ".mp4";
                    return ConvertFFmpeg(arguments);
                case "quicktime": 
                    arguments = "-i " + pathFile + " -vcodec copy -acodec copy " + pathFile + ".mp4";
                    return ConvertFFmpeg(arguments);
                case "x-matroska":
                    arguments = "-i " + pathFile + " -codec copy " + pathFile + ".mp4";
                    return ConvertFFmpeg(arguments);
                case "mp4": 
                    File.Move(pathFile, pathFile + ".mp4");
                    return true;
                default : 
                    log.Information("Can't define file type to convert file for auto-posting.");
                    return false;
            }
        }
        // public Stream GetVideoThumbnail(string pathFile)
        // {
        //     MemoryStream convertedFile = null;
        //     string thumbnailPath, arguments;
            
        //     thumbnailPath = Directory.GetCurrentDirectory() + "/" + DateTime.Now.Ticks.ToString() + "-tn.jpg";
        //     arguments = pathFile + "[1] " + thumbnailPath;

        //     if (ConvertImageMagick(arguments)) {
        //         using (Stream stream = File.Open(thumbnailPath, FileMode.Open)) {
        //             convertedFile = new MemoryStream();
        //             stream.CopyTo(convertedFile);
        //             convertedFile.Seek(0, SeekOrigin.Begin);
        //         }
        //         File.Delete(thumbnailPath);
        //     }
        //     return convertedFile;
        // }
        public Stream GetVideoThumbnail(string pathFile)
        {
            MemoryStream convertedFile = null;
            string thumbnailPath, arguments;
            
            thumbnailPath = Directory.GetCurrentDirectory() + "/" + DateTime.Now.Ticks.ToString() + "-tn.jpg";
            arguments = "-i " + pathFile + " -vframes 1 -an -ss 1 " + thumbnailPath;

            if (ConvertFFmpeg(arguments)) {
                using (Stream stream = File.Open(thumbnailPath, FileMode.Open)) {
                    convertedFile = new MemoryStream();
                    stream.CopyTo(convertedFile);
                    convertedFile.Seek(0, SeekOrigin.Begin);
                }
                File.Delete(thumbnailPath);
            }
            return convertedFile;
        }
        public bool ConvertImageMagick(string args)
        {
            try {
                using (Process process = new Process()) {
                    process.StartInfo.FileName = "convert";
                    process.StartInfo.Arguments = args;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                }
                return true;
            } 
            catch (Exception e) {
                log.Error("Can't convert file with args -> " + args + ". Message -> " + e.Message);
            }
            return false;
        }
        public bool ConvertFFmpeg(string args)
        {
            try {
                using (Process process = new Process()) {
                    process.StartInfo.FileName = FFmpegExe;
                    process.StartInfo.Arguments = args;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                }
                return true;
            } 
            catch (Exception e) {
                log.Error("Can't convert file with args -> " + args + ". Message -> " + e.Message);
            }
            return false;
        }
    }
}