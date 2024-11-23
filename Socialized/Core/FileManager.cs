using Serilog;

namespace Core
{
    public class FileManager : IFileManager
    {
        private readonly string currentDirectory = Directory.GetCurrentDirectory();
        public readonly ILogger Logger;
        public DateTime currentTime = DateTime.Now;
        public string dailyFolder = "/" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "/";

        public FileManager(ILogger logger)
        {
            Logger = logger;
        }
        public virtual string SaveFile(Stream file, string RelativePath)
        {
            string fileName = Guid.NewGuid().ToString();
            ChangeDailyPath();
            string fileRelativePath = "/" + RelativePath + dailyFolder;
            CheckDirectory(fileRelativePath);            
            if (SaveTo(file, fileRelativePath, fileName))
            {
                return fileRelativePath + fileName;
            }
            return null;
        }
        public virtual bool SaveTo(Stream file, string relativePath, string fileName)
        {
            if (!File.Exists(currentDirectory + relativePath + fileName))
            {
                using (var stream = new FileStream(currentDirectory + relativePath + fileName, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                Logger.Information("Create file, fileName ->" + fileName);
                return true;
            }
            else
            {
                Logger.Error("Server can't save file with same file names.");
            }
            return false;
        }
        /// <summary>
        /// Change daily path to save files in daily new folder. That need to save file without override another file.
        /// <summary>
        public void ChangeDailyPath()
        {
            if (currentTime.Day != DateTime.Now.Day) 
            {
                currentTime = DateTime.Now;
                dailyFolder = "/" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "/";
            }
        }
        public void CheckDirectory(string fileRelativePath)
        {
            if (!Directory.Exists(currentDirectory + fileRelativePath))
            {
                Directory.CreateDirectory(currentDirectory + fileRelativePath);
            }
        }
        public void DeleteFile(string relativePath)
        {
            if (File.Exists(currentDirectory + relativePath)) 
            {
                File.Delete(currentDirectory + relativePath);
                Logger.Information("File was deleted. Relative path ->/" + relativePath);
            }
        }
    }
}