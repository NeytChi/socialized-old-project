using System;
using System.IO;
using Serilog.Core;
using Microsoft.AspNetCore.Http;

namespace Common
{
    /// <summary>
    /// This class save files by specific method.
    /// <summary>
    public class FileManager
    {
        public FileManager(Logger log)
        {
            this.log = log;
        }
        public Logger log;
        private Random random = new Random();
        private string Alphavite = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        
        private string currentDirectory = Directory.GetCurrentDirectory();
        private DateTime currentTime = DateTime.Now;
        public string dailyFolder = "/" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "/";
        
        /// <summary>
        /// Save file by specific path.
        /// <summary>
        /// <param>Relative path without first and last '/'</param>
        /// <return>Relative file path.</return>
        public virtual string SaveFile(IFormFile file, string RelativePath)
        {
            string fileName = CreateHash(10);
            ChangeDailyPath();
            string fileRelativePath = "/" + RelativePath + dailyFolder;
            CheckDirectory(fileRelativePath);            
            if (SaveTo(file, fileRelativePath, fileName))
                return fileRelativePath + fileName;
            return null;
        }
        public virtual bool SaveTo(IFormFile file, string relativePath, string fileName)
        {
            if (!File.Exists(currentDirectory + relativePath + fileName)) {
                using (var stream = new FileStream(currentDirectory + relativePath + fileName, FileMode.Create))
                    file.CopyTo(stream);
                log.Information("Create file, fileName ->" + fileName);
                return true;
            }
            else
                log.Error("Server can't save file with same file names.");
            return false;
        }
        /// <summary>
        /// Change daily path to save files in daily new folder. That need to save file without override another file.
        /// <summary>
        public void ChangeDailyPath()
        {
            if (currentTime.Day != DateTime.Now.Day) {
                currentTime = DateTime.Now;
                dailyFolder = "/" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "/";
            }
        }
        public void CheckDirectory(string fileRelativePath)
        {
            if (!Directory.Exists(currentDirectory + fileRelativePath))
                Directory.CreateDirectory(currentDirectory + fileRelativePath);
        }
        public void DeleteFile(string relativePath)
        {
            if (File.Exists(currentDirectory + relativePath)) {
                File.Delete(currentDirectory + relativePath);
                log.Information("File was deleted. Relative path ->/" + relativePath);
            }
        }
        public string CreateHash(int lengthHash)
        {
            string hash = "";
            for (int i = 0; i < lengthHash; i++)
                hash += Alphavite[random.Next(Alphavite.Length)];
            return hash;
        }
    }
}